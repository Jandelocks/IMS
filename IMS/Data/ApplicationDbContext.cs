using IMS.Models;
using Microsoft.EntityFrameworkCore;
using System;

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
        public object User { get; internal set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<IncidentsModel>()
                .HasOne(i => i.User)
                .WithMany(u => u.Incidents)
                .HasForeignKey(i => i.user_id);
        }
    }
}
