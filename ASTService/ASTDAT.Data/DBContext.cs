using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASTDAT.Data.Models
{
    public class DBContext: DbContext
    {
        public DbSet<StateModel> States { get; set; }
        public DbSet<OriginDestinationModel> OriginDestinations { get; set; }
        public DbSet<AssetModel> Assets { get; set; }
        public DbSet<AssetCommentModel> AssetComments { get; set; }
        public DbSet<LoadModel> Loads { get; set; }
        public DbSet<LoadTypeModel> LoadTypes { get; set; }
        public DbSet<CompanyModel> Companies { get; set; }
        public DbSet<ImportLogModel> ImportLogs { get; set; }
        public DbSet<DATLoginModel> DATLogins { get; set; }
        public DbSet<TSLoginModel> TSLogins { get; set; }
        public DbSet<UploadLogModel> UploadLogs { get; set; }
        public DbSet<LoadHistoryModel> LoadHistory { get; set; }
        public DbSet<LocationModel> Locations { get; set; }
        public DbSet<LoadCommentModel> LoadComments { get; set; }
        public DbSet<LoadClaimModel> LoadClaims { get; set; }
        public DbSet<LoadTemplateModel> LoadTemplates { get; set; }

        public DBContext() : base("DefaultConnection")
        {

        }

        public DBContext(string cs) : base(cs)
        {

        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StateModel>()
                .ToTable("States");

            modelBuilder.Entity<OriginDestinationModel>()
                .ToTable("OriginDestination");

            modelBuilder.Entity<AssetModel>()
                .ToTable("Assets");

            modelBuilder.Entity<AssetCommentModel>()
                .ToTable("AssetComments");

            modelBuilder.Entity<LoadModel>()
                .ToTable("Loads");

            modelBuilder.Entity<LoadTypeModel>()
                .ToTable("LoadTypes");

            modelBuilder.Entity<CompanyModel>()
                .ToTable("Companies");

            modelBuilder.Entity<ImportLogModel>()
                .ToTable("ImportLogs");

            modelBuilder.Entity<DATLoginModel>()
                .ToTable("DATLogins");

            modelBuilder.Entity<TSLoginModel>()
                .ToTable("TSLogins");

            modelBuilder.Entity<UploadLogModel>()
                .ToTable("UploadLog");

            modelBuilder.Entity<LoadHistoryModel>()
                .ToTable("LoadHistory");

            modelBuilder.Entity<LocationModel>()
                .ToTable("Locations");

            modelBuilder.Entity<LoadCommentModel> ()
                .ToTable("LoadComments");

            modelBuilder.Entity<LoadClaimModel>()
                .ToTable("LoadClaims");

            modelBuilder.Entity<LoadTemplateModel>()
                .ToTable("LoadTemplates");

            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges()
        {
            try
            {
                return base.SaveChanges();
            }
            catch(DbEntityValidationException exc)
            {
                var message = "";
                foreach (var item in exc.EntityValidationErrors)
                {
                    message += $"{item.Entry.Entity.GetType().Name} = {String.Join(", ", item.ValidationErrors.Select(x => $"{x.PropertyName} {x.ErrorMessage}"))}\r\n";
                }

                //throw exc;
                throw new Exception(message, exc);
            }
            catch(Exception exc)
            {
                throw exc;
            }
        }
    }
}
