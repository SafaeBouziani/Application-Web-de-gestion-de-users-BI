using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UserManagementPBI.Models;

namespace UserManagementPBI.Data

{
    public class ApplicationDbContext: IdentityDbContext<Admins>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Admins> Admins { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<Reports> Reports { get; set; } 
        public DbSet<Reports_Reports_BI> Reports_Reports_BI { get; set; }
        public DbSet<Users_Reports> Users_Reports { get; set; } // This is not a DbSet, but a navigation property
        public DbSet<Catalog> Catalog { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Catalog>().HasKey(c => c.ItemID);


            modelBuilder.Entity<Users>()
                .HasQueryFilter(u => u.is_active)
                .HasOne(u => u.CreatedByAdmin)
                .WithMany(a => a.Users)
                .HasForeignKey(u => u.CreatedByAdminId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull); 


            modelBuilder.Entity<Users_Reports>()
            .HasQueryFilter(ur => ur.User.is_active)
            .HasQueryFilter(ur => ur.Report.is_active)
            .HasKey(ur => new { ur.id_users, ur.id_reports });

            modelBuilder.Entity<Users_Reports>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UsersReports)
                .HasForeignKey(ur => ur.id_users);

            modelBuilder.Entity<Users_Reports>()
                .HasOne(ur => ur.Report)
                .WithMany(r => r.UsersReports)
                .HasForeignKey(ur => ur.id_reports);

            modelBuilder.Entity<Reports>()
                .HasQueryFilter(r => r.is_active)
                .HasKey(r => r.ID);

            modelBuilder.Entity<Reports_Reports_BI>()
                .HasQueryFilter(rr => rr.is_active)
                .HasKey(rr => rr.ID_Reports_Reports_BI);

            // ReportsReportsBI: 1 Report to many BI reports
            modelBuilder.Entity<Reports_Reports_BI>()
                .HasOne(rr => rr.ReportGroup)
                .WithMany(r => r.ReportsBIs)
                .HasForeignKey(rr => rr.id_report)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

        }



    }


}
