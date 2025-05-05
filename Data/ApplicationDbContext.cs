using Microsoft.EntityFrameworkCore;
using server.Model;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BC = BCrypt.Net.BCrypt;

namespace server.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<EmailSettings> EmailSettings { get; set; }
        public DbSet<OTP> OTPs { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        // DbSet properties for each entity
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Tasks> Tasks { get; set; }

        public DbSet<DailyTaskHours> DailyTaskHours { get; set; }

        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamMember> TeamMembers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure EmailSettings as keyless entity
            modelBuilder.Entity<EmailSettings>().HasNoKey();

            // Set identity seed for primary keys
            modelBuilder.Entity<Employee>()
                .Property(e => e.EmployeeId)
                .UseIdentityColumn(1, 1);

            modelBuilder.Entity<Role>().HasData(
                new Role { RoleID = 3, RoleName = "Admin" },
                new Role { RoleID = 2, RoleName = "Manager" },
                new Role { RoleID = 1, RoleName = "User" }
            );

            modelBuilder.Entity<Employee>().HasData(
      new Employee
      {
          EmployeeId = 1,
          Username = "admin",
          Email = "admin@quadranttechnologies.com",
          Password = BC.HashPassword("admin"),
          RoleID = 3, 
          ManagerId = null, 
          JoinedDate = DateTime.UtcNow
      }
          );

            // Employee - Role (Many-to-One)
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Role)
                .WithMany(r => r.Employees)
                .HasForeignKey(e => e.RoleID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Tasks entity
            modelBuilder.Entity<Tasks>()
                .Property(t => t.TaskId)
                .UseIdentityColumn(1, 1);

             modelBuilder.Entity<Tasks>()
            .Property(t => t.priority) // Configure Priority if needed
            .IsRequired(); // Example: Make it a required field

            modelBuilder.Entity<Tasks>()
                .HasOne<Employee>()
                .WithMany()
                .HasForeignKey(t => t.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure self-referencing relationship for Employee
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Manager)
                .WithMany(m => m.ManagedEmployees)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);


            // Configure Team entity
            modelBuilder.Entity<Team>()
                .Property(t => t.TeamId)
                .UseIdentityColumn(1, 1);

            // Team - Manager (Many-to-One)
            modelBuilder.Entity<Team>()
                .HasOne(t => t.Manager)
                .WithMany()
                .HasForeignKey(t => t.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure TeamMember entity
            modelBuilder.Entity<TeamMember>()
                .Property(tm => tm.TeamMemberId)
                .UseIdentityColumn(1, 1);

            // TeamMember - Team (Many-to-One)
            modelBuilder.Entity<TeamMember>()
                .HasOne(tm => tm.Team)
                .WithMany(t => t.TeamMembers)
                .HasForeignKey(tm => tm.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            // TeamMember - Employee (Many-to-One)
            modelBuilder.Entity<TeamMember>()
                .HasOne(tm => tm.Employee)
                .WithMany()
                .HasForeignKey(tm => tm.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}