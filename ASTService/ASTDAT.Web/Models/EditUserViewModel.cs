using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ASTDAT.Web.Models
{
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required]
        [Display(Name = "User Name")]
        public string UserName { get; set; }
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; }

        //[StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
        public IEnumerable<SelectListItem> RolesList { get; set; }
        //public IEnumerable<SelectListItem> AgentList { get; set; }
        //public int AgentId { get; set; }
        [Display(Name = "Full Name")]
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Extension { get; set; }
        public string Email2 { get; set; }
        [Required]
        [StringLength(15)]
        public string Location { get; set; }
        public IEnumerable<SelectListItem> Locations { get; set; }
    }
}