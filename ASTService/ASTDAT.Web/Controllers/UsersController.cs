using ASTDAT.Data.Models;
using ASTDAT.Web.Infrastructure;
using ASTDAT.Web.Models;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ASTDAT.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        public UsersController()
        {

        }
        public UsersController(ApplicationUserManager userManager, ApplicationRoleManager roleManager)
        {
            UserManager = userManager;
            RoleManager = roleManager;
        }
        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        private ApplicationRoleManager _roleManager;
        public ApplicationRoleManager RoleManager
        {
            get
            {
                return _roleManager ?? HttpContext.GetOwinContext().Get<ApplicationRoleManager>();
            }
            private set
            {
                _roleManager = value;
            }
        }

        // GET: Users
        public async Task<ActionResult> Index()
        {
            var users = await UserManager.Users.OrderBy(x => x.UserName).ToListAsync();
            var companyIds = users.Select(u => u.CompanyId).Where(x => x != null).Select(x => x.Value).Distinct().ToList();
            Dictionary<int, Company> companyById;
            using (var app = new ApplicationDbContext())
            {
                companyById = app.Companies.Where(c => companyIds.Contains(c.Id)).ToList().ToDictionary(c => c.Id, c => c);
            }
            var vm = new List<EditUserViewModel>();
            foreach (var p in users)
            {
                var roleNames = await UserManager.GetRolesAsync(p.Id);
                Company comp = null;
                if (p.CompanyId != null) companyById.TryGetValue(p.CompanyId.Value, out comp);
                vm.Add(new EditUserViewModel
                {
                    Id = p.Id,
                    UserName = p.UserName,
                    Email = p.Email,
                    FullName = p.FullName,
                    CarrierApprovalStatus = p.CarrierApprovalStatus,
                    IsCarrierUser = roleNames.Contains("Carrier"),
                    CompanyId = p.CompanyId,
                    CompanyName = comp?.Name,
                    CompanyType = comp?.CompanyType,
                    CompanyOnboardingStatus = comp?.OnboardingStatus,
                    RolesList = RoleManager.Roles.OrderBy(o => o.Name).Where(r => r.Users.Any(u => u.UserId == p.Id)).ToList().Select(x => new SelectListItem
                    {
                        Text = x.Name.Contains("Approver") ? "Portal Approver" : x.Name,
                        Value = x.Name
                    })
                });
            }

            return View(vm);
        }

        public async Task<ActionResult> Create()
        {
            var db = new DBContext();
            //ViewBag.Agents = new SelectList(db.Agents.ToList(), "AgentId", "Name");
            //Get the list of Roles
            //ViewBag.RoleId = new SelectList(await RoleManager.Roles.ToListAsync(), "Name", "Name");
            ViewBag.RoleId = RoleManager.Roles.ToList().Select(x => new SelectListItem()
            {
                Selected = x.Name == "Shipper",
                Text = x.Name,
                Value = x.Name
            }).ToList();
            ViewBag.Locations = new SelectList(db.Locations.OrderBy(x => x.Location).ToList(), "Location", "Location");
            using (var idDb = new ApplicationDbContext())
            {
                ViewBag.Companies = new SelectList(idDb.Companies.OrderBy(c => c.Name).ToList(), "Id", "Name");
            }
            return View();
        }

        // POST: /Users/Create
        [HttpPost]
        public async Task<ActionResult> Create(RegisterViewModel userViewModel, params string[] selectedRoles)
        {
            var db = new DBContext();
            selectedRoles = selectedRoles ?? new string[] { };
            ViewBag.SelectedRoles = selectedRoles;
            ViewBag.RoleId = RoleManager.Roles.ToList().Select(x => new SelectListItem()
            {
                Selected = selectedRoles.Contains(x.Name) || (selectedRoles.Length == 0 && x.Name == "Shipper"),
                Text = x.Name,
                Value = x.Name
            }).ToList();
            ViewBag.Locations = new SelectList(db.Locations.OrderBy(x => x.Location).ToList(), "Location", "Location");
            using (var idDb = new ApplicationDbContext())
            {
                ViewBag.Companies = new SelectList(idDb.Companies.OrderBy(c => c.Name).ToList(), "Id", "Name");
            }

            if (selectedRoles == null || selectedRoles.Length == 0)
            {
                selectedRoles = new[] { "Shipper" };
            }

            var roleError = ValidateRoleRules(selectedRoles, userViewModel.CompanyId, userViewModel.NewCompanyName, userViewModel.NewCompanyType);
            if (roleError != null) ModelState.AddModelError("", roleError);

            int? resolvedCompanyId = userViewModel.CompanyId;
            if (ModelState.IsValid && !string.IsNullOrWhiteSpace(userViewModel.NewCompanyName) && !string.IsNullOrWhiteSpace(userViewModel.NewCompanyType))
            {
                using (var idDb = new ApplicationDbContext())
                {
                    var comp = new Company
                    {
                        Name = userViewModel.NewCompanyName.Trim(),
                        CompanyType = userViewModel.NewCompanyType,
                        OnboardingStatus = OnboardingStatuses.Pending,
                        CreatedUtc = DateTime.UtcNow
                    };
                    idDb.Companies.Add(comp);
                    idDb.SaveChanges();
                    resolvedCompanyId = comp.Id;
                }
            }

            if (ModelState.IsValid)
            {
                if (selectedRoles.Contains("Dispatcher") && resolvedCompanyId == null)
                    ModelState.AddModelError("", "Dispatcher must be linked to a company (select existing or create new).");
                if ((selectedRoles.Contains("Shipper") || selectedRoles.Contains("Carrier")) && resolvedCompanyId == null)
                    ModelState.AddModelError("", "Shipper and carrier users must belong to a company (create new or pick existing).");
            }

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = userViewModel.UserName,
                    Email = userViewModel.Email,
                    FullName = userViewModel.FullName,
                    Phone = userViewModel.Phone,
                    Extension = userViewModel.Extension,
                    Email2 = userViewModel.Email2,
                    Location = userViewModel.Location,
                    CompanyId = resolvedCompanyId
                };
                if (selectedRoles.Contains("Carrier")) user.CarrierApprovalStatus = "Pending";

                var adminresult = await UserManager.CreateAsync(user, userViewModel.Password);

                if (adminresult.Succeeded)
                {
                    if (selectedRoles != null)
                    {
                        var result = await UserManager.AddToRolesAsync(user.Id, selectedRoles);
                        if (!result.Succeeded)
                        {
                            ModelState.AddModelError("", result.Errors.First());
                            return View();
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError("", adminresult.Errors.First());
                    return View();
                }
                return RedirectToAction("Index");
            }

            return View();
        }

        public async Task<ActionResult> Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            var userRoles = await UserManager.GetRolesAsync(user.Id);

            var db = new DBContext();

            Company comp = null;
            if (user.CompanyId != null)
            {
                using (var idDb = new ApplicationDbContext())
                {
                    comp = idDb.Companies.Find(user.CompanyId);
                }
            }

            var vm = new EditUserViewModel()
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                Extension = user.Extension,
                Location = user.Location,
                Email2 = user.Email2,
                CarrierApprovalStatus = user.CarrierApprovalStatus,
                IsCarrierUser = userRoles.Contains("Carrier"),
                CompanyId = user.CompanyId,
                CompanyName = comp?.Name,
                CompanyType = comp?.CompanyType,
                CompanyOnboardingStatus = comp?.OnboardingStatus,
                Locations = new SelectList(db.Locations.OrderBy(x => x.Location).ToList(), "Location", "Location"),
                RolesList = RoleManager.Roles.ToList().Select(x => new SelectListItem()
                {
                    Selected = userRoles.Contains(x.Name),
                    Text = x.Name,
                    Value = x.Name
                })
            };

            ViewBag.CarrierStatuses = new SelectList(
                CarrierApprovalStatuses.All.Select(s => new { Value = s, Text = s }),
                "Value", "Text", user.CarrierApprovalStatus ?? CarrierApprovalStatuses.Pending);
            ViewBag.CompanyOnboardingStatuses = new SelectList(
                OnboardingStatuses.All.Select(s => new { Value = s, Text = s }),
                "Value", "Text", comp?.OnboardingStatus ?? OnboardingStatuses.Pending);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(EditUserViewModel editUser, params string[] selectedRole)
        {
            selectedRole = selectedRole ?? new string[] { };
            ViewBag.SelectedRoles = selectedRole;
            var db = new DBContext();

            ViewBag.Locations = new SelectList(db.Locations.OrderBy(x => x.Location).ToList(), "Location", "Location");
            editUser.Locations = new SelectList(db.Locations.OrderBy(x => x.Location).ToList(), "Location", "Location");
            var userRoles = await UserManager.GetRolesAsync(editUser.Id);

            editUser.RolesList = RoleManager.Roles.ToList().Select(x => new SelectListItem()
            {
                Selected = userRoles.Contains(x.Name) || selectedRole.Contains(x.Name),
                Text = x.Name,
                Value = x.Name
            });

            var userBeingEditedPre = await UserManager.FindByIdAsync(editUser.Id);
            PrepareCarrierStatusesOnEdit(editUser, userBeingEditedPre, userRoles);

            var carrierShipperErrorEdit = ValidateCarrierShipperExclusive(selectedRole);
            if (carrierShipperErrorEdit != null)
            {
                ModelState.AddModelError("", carrierShipperErrorEdit);
            }

            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByIdAsync(editUser.Id);
                if (user == null)
                {
                    return HttpNotFound();
                }

                user.Email = editUser.Email;
                //user.UserName = editUser.Email;
                user.FullName = editUser.FullName;
                user.Phone = editUser.Phone;
                user.Extension = editUser.Extension;
                user.Email2 = editUser.Email2;
                user.Location = editUser.Location;
                if (!string.IsNullOrWhiteSpace(editUser.Password))
                {
                    //if (editUser.Password != editUser.ConfirmPassword)
                    //{
                    //    ModelState.AddModelError("", "The password and confirmation password do not match.");
                    //    return View(editUser);
                    //}

                    string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                    var resultpass = await UserManager.ResetPasswordAsync(user.Id, code, editUser.Password);

                    if (!resultpass.Succeeded)
                    {
                        ModelState.AddModelError("", resultpass.Errors.First());
                        return View(editUser);
                    }
                }

                var result = await UserManager.AddToRolesAsync(user.Id, selectedRole.Except(userRoles).ToArray<string>());

                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", result.Errors.First());
                    return View(editUser);
                }
                result = await UserManager.RemoveFromRolesAsync(user.Id, userRoles.Except(selectedRole).ToArray<string>());

                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", result.Errors.First());
                    return View(editUser);
                }

                var updatedRoles = await UserManager.GetRolesAsync(user.Id);
                if (updatedRoles.Contains("Carrier"))
                {
                    if (string.IsNullOrEmpty(user.CarrierApprovalStatus))
                    {
                        user.CarrierApprovalStatus = CarrierApprovalStatuses.Pending;
                    }
                    if (LoadboardPermissions.CanApproveUsersAndCompanies(User)
                        && !string.IsNullOrEmpty(editUser.CarrierApprovalStatus)
                        && CarrierApprovalStatuses.All.Contains(editUser.CarrierApprovalStatus))
                    {
                        user.CarrierApprovalStatus = editUser.CarrierApprovalStatus;
                    }
                }
                else
                {
                    user.CarrierApprovalStatus = null;
                }

                if (user.CompanyId != null && LoadboardPermissions.CanApproveUsersAndCompanies(User) && !string.IsNullOrEmpty(editUser.CompanyOnboardingStatus))
                {
                    var normalized = OnboardingStatuses.Normalize(editUser.CompanyOnboardingStatus);
                    if (!string.IsNullOrEmpty(normalized) && OnboardingStatuses.All.Contains(normalized))
                    {
                        using (var idDb = new ApplicationDbContext())
                        {
                            var c = idDb.Companies.Find(user.CompanyId.Value);
                            if (c != null)
                            {
                                c.OnboardingStatus = normalized;
                                idDb.SaveChanges();
                            }
                        }
                    }
                }

                await UserManager.UpdateAsync(user);
                return RedirectToAction("Index");
            }
            ModelState.AddModelError("", "Something failed.");
            return View(editUser);
        }

        public async Task<ActionResult> Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        //
        // POST: /Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(string id)
        {
            if (ModelState.IsValid)
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                var user = await UserManager.FindByIdAsync(id);
                if (user == null)
                {
                    return HttpNotFound();
                }
                var result = await UserManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", result.Errors.First());
                    return View();
                }
                return RedirectToAction("Index");
            }
            return View();
        }

        private static string ValidateRoleRules(string[] roles, int? companyId, string newCompanyName, string newCompanyType)
        {
            roles = roles ?? new string[0];
            if (roles.Contains("Carrier") && roles.Contains("Shipper"))
            {
                return "A user cannot be both Carrier and Shipper. Choose one (or staff roles without mixing those two).";
            }
            if (roles.Contains("Dispatcher") && (roles.Contains("Shipper") || roles.Contains("Carrier") || roles.Contains("Admin")))
            {
                return "Dispatcher must be a single sub-role: select only Dispatcher, together with a company link.";
            }
            if (roles.Contains("Dispatcher") && companyId == null && string.IsNullOrWhiteSpace(newCompanyName))
            {
                return "For Dispatcher, select an existing company, or create a new company and type.";
            }
            if ((roles.Contains("Shipper") || roles.Contains("Carrier")) && string.IsNullOrWhiteSpace(newCompanyName) && (companyId == null))
            {
                return "Shipper/Carrier: pick an existing company, or create one with new company name and type.";
            }
            return null;
        }

        private static string ValidateCarrierShipperExclusive(string[] roles)
        {
            roles = roles ?? new string[0];
            if (roles.Contains("Carrier") && roles.Contains("Shipper"))
            {
                return "A user cannot be both Carrier and Shipper. Choose one (or staff roles without mixing those two).";
            }
            return null;
        }

        private void PrepareCarrierStatusesOnEdit(EditUserViewModel editUser, ApplicationUser dbUser, IList<string> roles)
        {
            editUser.IsCarrierUser = roles != null && roles.Contains("Carrier");
            if (string.IsNullOrEmpty(editUser.CarrierApprovalStatus) && dbUser != null)
            {
                editUser.CarrierApprovalStatus = dbUser.CarrierApprovalStatus;
            }
            ViewBag.CarrierStatuses = new SelectList(
                CarrierApprovalStatuses.All.Select(s => new { Value = s, Text = s }),
                "Value", "Text", editUser.CarrierApprovalStatus ?? CarrierApprovalStatuses.Pending);
            ViewBag.CompanyOnboardingStatuses = new SelectList(
                OnboardingStatuses.All.Select(s => new { Value = s, Text = s }),
                "Value", "Text", editUser.CompanyOnboardingStatus ?? OnboardingStatuses.Pending);
        }
    }
}