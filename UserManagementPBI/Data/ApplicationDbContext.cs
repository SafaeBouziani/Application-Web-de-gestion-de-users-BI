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
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Users>()
                .HasOne(u => u.CreatedByAdmin)
                .WithMany(a => a.Users)
                .HasForeignKey(u => u.CreatedByAdminId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction); // Or ClientSetNull if you want EF to handle orphaning

            // Avoid generating a foreign key constraint in the DB
            modelBuilder.Entity<Users>()
                .Ignore(u => u.CreatedByAdmin);

        }



    }


}
