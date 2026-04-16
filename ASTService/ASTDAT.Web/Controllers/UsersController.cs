using ASTDAT.Data.Models;
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
        public ActionResult Index()
        {
            var vm = UserManager.Users.Select(p =>
               new EditUserViewModel()
               {
                   Id = p.Id,
                   UserName = p.UserName,
                   Email = p.Email,
                   FullName = p.FullName,
                   RolesList = RoleManager.Roles.OrderBy(o => o.Name).Where(r => r.Users.Any(u => u.UserId == p.Id)).ToList().Select(x => new SelectListItem()
                   {
                       Text = x.Name.Contains("Approver") ? "Portal Approver" : x.Name,
                       Value = x.Name
                   })
               }).ToList();

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
                Selected = false,
                Text = x.Name,
                Value = x.Name
            }).ToList();
            ViewBag.Locations = new SelectList(db.Locations.OrderBy(x => x.Location).ToList(), "Location", "Location");
            return View();
        }

        // POST: /Users/Create
        [HttpPost]
        public async Task<ActionResult> Create(RegisterViewModel userViewModel, params string[] selectedRoles)
        {
            var db = new DBContext();
            selectedRoles = selectedRoles ?? new string[] { };
            ViewBag.SelectedRoles = selectedRoles;
            //ViewBag.RoleId = new SelectList(await RoleManager.Roles.ToListAsync(), "Name", "Name");
            ViewBag.RoleId = RoleManager.Roles.ToList().Select(x => new SelectListItem()
            {
                Selected = selectedRoles.Contains(x.Name),
                Text = x.Name,
                Value = x.Name
            }).ToList();
            ViewBag.Locations = new SelectList(db.Locations.OrderBy(x => x.Location).ToList(), "Location", "Location");

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser {
                    UserName = userViewModel.UserName,
                    Email = userViewModel.Email,
                    FullName = userViewModel.FullName,
                    Phone = userViewModel.Phone,
                    Extension = userViewModel.Extension,
                    Email2 = userViewModel.Email2,
                    Location = userViewModel.Location,
                };
                var adminresult = await UserManager.CreateAsync(user, userViewModel.Password);

                //Add User to the selected Roles 
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
                Locations = new SelectList(db.Locations.OrderBy(x => x.Location).ToList(), "Location", "Location"),
                RolesList = RoleManager.Roles.ToList().Select(x => new SelectListItem()
                {
                    Selected = userRoles.Contains(x.Name),
                    Text = x.Name,
                    Value = x.Name
                })
            };


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
    }
}