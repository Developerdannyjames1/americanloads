using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace ASTDAT.Web.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        [Display(Name = "Name")]
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Extension { get; set; }
        public string Email2 { get; set; }
        [StringLength(15)]
        public string Location { get; set; }

        /// <summary>
        /// For users in the Carrier role: Pending, Approved, Rejected, Suspended. Null for non-carriers.
        /// </summary>
        [StringLength(32)]
        public string CarrierApprovalStatus { get; set; }

        /// <summary>Shipper or carrier company; users inherit company onboarding/permissions.</summary>
        public int? CompanyId { get; set; }
        public virtual Company Company { get; set; }

        //public int? AgentId { get; set; }
        //public virtual Agent Agent { get; set; }

        //public Guid? Token { get; set; }
        //public DateTime? TokenDateTime { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public DbSet<Company> Companies { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ApplicationUser>()
                .HasOptional(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId);
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}