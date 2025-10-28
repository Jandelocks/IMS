using IMS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NuGet.Common;
using System.Security.Cryptography;

namespace IMS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<UsersModel> users { get; set; }
        public DbSet<LogsModel> logs { get; set; }
        public DbSet<IncidentsModel> incidents { get; set; }
        public DbSet<UpdatesModel> updates { get; set; }
        public DbSet<CommentsModel> comments { get; set; }
        public DbSet<CategoriesModel> categories { get; set; }
        public DbSet<AttachmentsModel> attachments { get; set; }
        public DbSet<DepartmentsModel> departments { get; set; }
        public DbSet<NotificationsModel> Notifications { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Users -> Notifications (One-to-Many)
            modelBuilder.Entity<NotificationsModel>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.user_id)
                .OnDelete(DeleteBehavior.Cascade);

            // Department -> Categories (One-to-Many)
            modelBuilder.Entity<CategoriesModel>()
                .HasOne(c => c.Department)
                .WithMany(d => d.Categories)
                .HasForeignKey(c => c.department_id)
                .OnDelete(DeleteBehavior.Cascade); // If department is deleted, delete categories too

            // Relationship: Department → Incidents (One-to-Many)
            modelBuilder.Entity<IncidentsModel>()
               .HasOne(i => i.Department)
               .WithMany(d => d.Incidents)
               .HasForeignKey(i => i.department_id)
               .OnDelete(DeleteBehavior.Cascade); // If a department is deleted, delete related incidents

            // Relationship: Incident → User (One-to-Many)
            modelBuilder.Entity<IncidentsModel>()
                .HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.user_id)
                .OnDelete(DeleteBehavior.Restrict); // Prevent user deletion if incidents exist

            // Incidents - Users (Reporter)
            modelBuilder.Entity<IncidentsModel>()
                .HasOne(i => i.User)
                .WithMany(u => u.Incidents)
                .HasForeignKey(i => i.user_id)
                .OnDelete(DeleteBehavior.Cascade);

            // Updates - Users
            modelBuilder.Entity<UpdatesModel>()
                .HasOne(u => u.User)
                .WithMany(u => u.Updates)
                .HasForeignKey(u => u.user_id)
                .OnDelete(DeleteBehavior.Restrict);

            // Updates - Incidents (One-to-Many)
            modelBuilder.Entity<IncidentsModel>()
                .HasMany(i => i.Updates)
                .WithOne(u => u.Incident)
                .HasForeignKey(u => u.incident_id)
                .OnDelete(DeleteBehavior.Cascade);

            // Comments - Users
            modelBuilder.Entity<CommentsModel>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.user_id)
                .OnDelete(DeleteBehavior.Restrict);

            // Comments - Incidents
            modelBuilder.Entity<CommentsModel>()
                .HasOne(c => c.Incident)
                .WithMany()
                .HasForeignKey(c => c.incident_id)
                .OnDelete(DeleteBehavior.Cascade);

            // Attachments - Users
            modelBuilder.Entity<AttachmentsModel>()
                .HasOne(a => a.User)
                .WithMany(u => u.Attachments)
                .HasForeignKey(a => a.user_id)
                .OnDelete(DeleteBehavior.Restrict);

            // Attachments - Incidents
            modelBuilder.Entity<AttachmentsModel>()
                .HasOne(a => a.Incident)
                .WithMany()
                .HasForeignKey(a => a.incident_id)
                .OnDelete(DeleteBehavior.Cascade);

            //Logs - Users
            modelBuilder.Entity<LogsModel>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.user_id)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
