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
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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

            var passwordHasher = new PasswordHasher<UsersModel>();
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            modelBuilder.Entity<DepartmentsModel>().HasData(
               new DepartmentsModel
               {
                   department_id = 12,
                   department = "Information Technology (IT) Department",
                   description = "Responsible for maintaining and securing the organization's IT infrastructure, ensuring smooth operation of systems, networks, and software.",
                   token = "EYcLYD+5MDZefgLOMIoszTVmLL6YuHDc/r7gSnhjOMI=",
                   ImagePath = "/departments/IT.webp"
               },
               new DepartmentsModel
               {
                   department_id = 13,
                   department = "Human Resources (HR) Department",
                   description = "Handles employee-related issues, including recruitment, workplace policies, and conflict resolution.",
                   token = "1L9JFDAOhdRAfr8WTE2TFseSIbmFznhF8G+YQYeQztU=",
                   ImagePath = "/departments/hr.webp"
               },
               new DepartmentsModel
               {
                   department_id = 14,
                   department = "Facilities Management Department",
                   description = "Ensures the maintenance and safety of physical office spaces, including repairs and environmental concerns.",
                   token = "6G7QdUkik1mqrvrGW7onUkJgMDX0uWCx8eO5CGPAafI=",
                   ImagePath = "/departments/facilities.webp"
               },
               new DepartmentsModel
               {
                   department_id = 15,
                   department = "Customer Support Department",
                   description = "Addresses customer complaints, inquiries, and service-related issues to maintain customer satisfaction.",
                   token = "WKGIl7ur+NipjdwcCelAQMAWgwI4+yUKRAUJ+rwnUrI=",
                   ImagePath = "/departments/customer.webp"
               },
               new DepartmentsModel
               {
                   department_id = 16,
                   department = "Health and Safety Department",
                   description = "Ensures workplace safety, compliance with health regulations, and handles medical emergencies.",
                   token = "CfNwST/Pp0U9H/1bbkTQOQ308K05oiLvpcTGdLosZNA=",
                   ImagePath = "/departments/health and safety.webp"
               }
           );

            modelBuilder.Entity<UsersModel>().HasData(
                new UsersModel
                {
                    user_id = 1,
                    full_name = "Admin",
                    email = "admin@gmail.com",
                    password = "$2a$11$A.0BKl3ASTqoRr8dLlWoPesgiVtaDMYEklsW.FqVYeaGEPKc.Djwi",
                    role = "admin",
                    department = "NONE",
                    created_at = DateTime.Parse("2025-03-13 02:15:07"),
                    token = "b4DmKmCKT3AwPc+6bLF60/wFgDmXGnfn8We9F31R8EA=",
                    token_forgot = null,
                    isRistrict = false,
                    profile = "/uploads/db682a3e-8bd0-48de-986a-b89a678549ba_admin.jpg"
                },
                new UsersModel
                {
                    user_id = 3,
                    full_name = "Jandel L. Escalera",
                    email = "escalerajandel@gmail.com",
                    password = "$2a$11$Vn.lT8jQb5V/SotnvF/JGe4xSaGq2x6KFQP40rdkNxqee3YE4gERK",
                    role = "user",
                    department = "IT",
                    created_at = DateTime.Parse("2025-03-13 02:38:39"),
                    token = "MSK56cHgAJi9kXNzg2PRB0sLzVtP9Z4ywk4lPHWUHLw=",
                    token_forgot = null,
                    isRistrict = false,
                    profile = null
                },
                new UsersModel
                {
                    user_id = 4,
                    full_name = "Moderator",
                    email = "moderator@gmail.com",
                    password = "$2a$11$5p8lht/am/uAg96zUZpH4.ppxLkjcDstGjs5As/iNeuOTrVKMw/xa",
                    role = "moderator",
                    department = "Information Technology (IT) Department",
                    created_at = DateTime.Parse("2025-03-14 03:12:45"),
                    token = "HpI5vTPrFIYF9o0SYZ2lL3/3fWjJqJQQf+OF5G1JBqE=",
                    token_forgot = null,
                    isRistrict = false,
                    profile = null
                },
                new UsersModel
                {
                    user_id = 5,
                    full_name = "Moderator2",
                    email = "moderator2@gmail.com",
                    password = "$2a$11$OPYGmhz7SBSyDcF/og2dZ.pZIYHYJFKMowMKo0yId24MOx1doeVNa",
                    role = "moderator",
                    department = "Security Operations (SecOps)",
                    created_at = DateTime.Parse("2025-03-14 05:19:22"),
                    token = "fh3mxIB++x2xSNY8h52gX0cGNImmEZc4EDRx9Nzq+js=",
                    token_forgot = null,
                    isRistrict = false,
                    profile = null
                },
                new UsersModel
                {
                    user_id = 6,
                    full_name = "Maria Santos",
                    email = "maria.santos@example.com",
                    password = "$2a$11$/8EmvCVReBigVd5dTjbQmeatoWgapeEkclKXvY9G1FJ4xMi3IV442",
                    role = "user",
                    department = "NONE",
                    created_at = DateTime.Parse("2025-03-14 06:03:46"),
                    token = "ebISNl+Q/RQUtwr4BL8JVWpaFMpW8HocmMXf2viJA+A=",
                    token_forgot = null,
                    isRistrict = false,
                    profile = null
                },
                new UsersModel
                {
                    user_id = 7,
                    full_name = "James Rodriguez",
                    email = "james.rodriguez@example.com",
                    password = "$2a$11$7dmhE9tPbZWCsP3EmdlwPOfGZ3Lgas.S/P0A3Gbb8IF/LqwQfqiQO",
                    role = "user",
                    department = "NONE",
                    created_at = DateTime.Parse("2025-03-14 06:05:21"),
                    token = "38rpDGoPp9nOZcCkeul3zmUy+qxEbW5sHwqvDb3x6fg=",
                    token_forgot = null,
                    isRistrict = false,
                    profile = null
                },
                new UsersModel
                {
                    user_id = 8,
                    full_name = "Angela Lopez",
                    email = "angela.lopez@example.com",
                    password = "$2a$11$FRYxXUpMCqCvsLPrFd8rGOmNgLMjrHwbXbmoughA4W2vgiOHDNEhO",
                    role = "user",
                    department = "NONE",
                    created_at = DateTime.Parse("2025-03-14 06:07:19"),
                    token = "LvDhX4JQEvXfy7GwA0sBt8XDvg3GMIiZpomxNcPkw2Y=",
                    token_forgot = null,
                    isRistrict = false,
                    profile = null
                },
                new UsersModel
                {
                    user_id = 9,
                    full_name = "Mark Villanueva",
                    email = "mark.villanueva@example.com",
                    password = "$2a$11$kcWTJg9vJlxTwfEaroqEReRpzSFioLPjlsVhfDnro1AvRe.U5XEPC",
                    role = "user",
                    department = "NONE",
                    created_at = DateTime.Parse("2025-03-14 06:09:12"),
                    token = "oQRPEsk16kyTQDIBiLmfDkG63sWdsq/ckVlxHKNSk6o=",
                    token_forgot = null,
                    isRistrict = false,
                    profile = null
                }
            );

            modelBuilder.Entity<CategoriesModel>().HasData
                (
                    new CategoriesModel { category_id = 11, category_name = "Network Issues", description = "Problems related to connectivity, slow internet, or outages.", token = "y9OZPbXXJSbnO3rdh3F6gsb2dzHxtrhV7rQhbDv1XI0=", department_id = 12 },
                    new CategoriesModel { category_id = 12, category_name = "Software Bugs", description = "Errors, crashes, or failures in company software.", token = "mtgoEtuRj5REx26u0DNqVsxFHWJu9bKlrlbwggkVyBY=", department_id = 12 },
                    new CategoriesModel { category_id = 13, category_name = "Hardware Failures", description = "Malfunctions in computers, printers, or servers.", token = "QXeqHRMrRNVDF0yeEbD9S6USghh/o/lC7zZxDjfPFro=", department_id = 12 },
                    new CategoriesModel { category_id = 14, category_name = "Cybersecurity Threats", description = "Phishing attacks, malware, or unauthorized access.", token = "EJuQxkEAHY6T0HYQsX3VPgwS4c8s/rvnrRPsu8nRRW4=", department_id = 12 },
                    new CategoriesModel { category_id = 15, category_name = "Workplace Harassment", description = "Reports of discrimination, bullying, or misconduct", token = "dc4groq/aaMlmRUx9df/8I6aZUuuGzXsWORVWB8iHsM=", department_id = 13 },
                    new CategoriesModel { category_id = 16, category_name = "Payroll Issues", description = "Salary discrepancies, tax concerns, or missing payments", token = "MIclgIu/YLVP+tzB51rAfKHwGHCWkOvt+Px/LHUfS7c=", department_id = 13 },
                    new CategoriesModel { category_id = 17, category_name = "Attendance & Leave", description = "Problems with leave applications, tardiness tracking, or shift scheduling", token = "NMjqjhP7u+1PywbJw3/wwWfguXP+bcSP3eUZzsV3xJw=", department_id = 13 },
                    new CategoriesModel { category_id = 18, category_name = "Employee Benefits", description = "Issues related to healthcare, insurance, or retirement plans.", token = "ibEe5hUL5PZGMXcUJflXDcVI3PfPKLJGEuIgEtxYSX8=", department_id = 13 },
                    new CategoriesModel { category_id = 19, category_name = "Building Maintenance", description = "Repairs for broken equipment, lighting, or air conditioning.", token = "yeYHizT9QjRo9RGoY5HzYzInthphnSVppTstTIKVdd4=", department_id = 14 },
                    new CategoriesModel { category_id = 20, category_name = "Safety Hazards", description = "Fire hazards, exposed wiring, or structural issues", token = "gK34Sfu/ziM6oyMbdOgIXgvRySa9lHqNusOmEUohCVs=", department_id = 14 },
                    new CategoriesModel { category_id = 21, category_name = "Janitorial Services", description = "Complaints regarding cleanliness or waste disposal.", token = "eFXqvuloVhXkT+Zq/aU1jaw/cfP1PqMtD4gUCM+hLpI=", department_id = 14 },
                    new CategoriesModel { category_id = 22, category_name = "Security Concerns", description = "Issues with access control, surveillance, or unauthorized entry.", token = "rvccCYQUAkBw7mJAnwyncMzk6QlDsaM+Y8S/linexjU=", department_id = 14 },
                    new CategoriesModel { category_id = 23, category_name = "Product Defects", description = "Issues with faulty or malfunctioning products", token = "hy6uZg8nR1nmym+b2UBrBOJiYCE6wzVmH2+SaPHUhD4=", department_id = 15 },
                    new CategoriesModel { category_id = 24, category_name = "Billing Disputes", description = "Overcharges, incorrect invoices, or refunds.", token = "l+fXeUVOHJLJMdDyFgbCvBw7Xo+AH8z+T6jHEbyncCI=", department_id = 15 },
                    new CategoriesModel { category_id = 25, category_name = "Service Complaints", description = "Delayed deliveries, poor service, or unresponsive support.", token = "NDxdk+RMKCooSgYQnRbs7eR54lNvdIxBn0tPpb1jVlA=", department_id = 15 },
                    new CategoriesModel { category_id = 26, category_name = "Account Issues", description = "Problems with login, password resets, or account suspension", token = "XndG/Zx19NQ+95a1Kz5ix5Hmo68zFiR8oPcPJk1eFp4=", department_id = 15 },
                    new CategoriesModel { category_id = 27, category_name = "Workplace Injuries", description = "Reporting accidents, injuries, or hazardous conditions", token = "dbjQ/wTKDNor9ZCFZl38BZrGscycG7vWYspk1i1IkpM=", department_id = 16 },
                    new CategoriesModel { category_id = 28, category_name = "Health Code Violations", description = "Unsanitary conditions, food safety, or medical concerns.", token = "3qQfElbe1OmAe3g2YxdrvYRHNRpHbqlXnANpenEmkV8=", department_id = 16 },
                    new CategoriesModel { category_id = 29, category_name = "Fire Safety Issues", description = "Fire alarm malfunctions, blocked exits, or fire drill concerns.", token = "Y6gxXphSXY/BwSBUgULKKnHY1nQN4ioEyRJq9tR1cQM=", department_id = 16 },
                    new CategoriesModel { category_id = 30, category_name = "Ergonomic Concerns", description = "Poor workstation setup causing discomfort or injury", token = "ppDJseg+d8dPcOrHewCLpayXF91upPU0UlOvpT+V43o=", department_id = 16 }
                );

            modelBuilder.Entity<IncidentsModel>().HasData(
                new IncidentsModel
                {
                    incident_id = 12,
                    user_id = 3,
                    tittle = "Network Outage in Main Office",
                    description = "The entire office lost internet connectivity around 10:30 AM. All employees are unable to access internal systems, emails, and cloud-based services. Restarting the router did not resolve the issue.",
                    status = "Pending",
                    priority = "High",
                    category = "Network Issues",
                    reported_at = DateTime.Parse("2025-03-14 06:02:22"),
                    assigned_too = null,
                    token = "Qd/aT69LhRylvJFVd+GsPfEcSNgmKR1z+xX+0Z4zB/Y=",
                    updated_at = null,
                    department_id = 12
                },
                new IncidentsModel
                {
                    incident_id = 13,
                    user_id = 6,
                    tittle = "Payroll System Login Failure",
                    description = "HR employees are unable to log in to the payroll system. Error message: 'Invalid Credentials' even though the credentials are correct.",
                    status = "Pending",
                    priority = "High",
                    category = "Payroll Issues",
                    reported_at = DateTime.Parse("2025-03-14 06:04:55"),
                    assigned_too = null,
                    token = "UptZquhuvLGhM01qb1cmd9V9oPQKl0cuAx6GXsYH9RE=",
                    updated_at = null,
                    department_id = 13
                },
                new IncidentsModel
                {
                    incident_id = 14,
                    user_id = 7,
                    tittle = "Elevator Malfunction in Building B",
                    description = "Elevator in Building B is stuck on the 3rd floor. Employees are unable to use it, and the control panel is unresponsive.",
                    status = "Pending",
                    priority = "High",
                    category = "Building Maintenance",
                    reported_at = DateTime.Parse("2025-03-14 06:06:41"),
                    assigned_too = null,
                    token = "Q/8L3vjijELj2ApZvZ/RtlpzPNIsD/86Hq8D0vc7bHs=",
                    updated_at = null,
                    department_id = 14
                },
                new IncidentsModel
                {
                    incident_id = 15,
                    user_id = 8,
                    tittle = "Customer Complaint System Not Responding",
                    description = "The customer complaint tracking system crashes after submitting a complaint. This is affecting response times.",
                    status = "Pending",
                    priority = "High",
                    category = "Account Issues",
                    reported_at = DateTime.Parse("2025-03-14 06:08:45"),
                    assigned_too = null,
                    token = "5Zb+X3izbPijUY8celgVJ40PefnDxz75PSUdh6AcVbU=",
                    updated_at = null,
                    department_id = 15
                },
                new IncidentsModel
                {
                    incident_id = 16,
                    user_id = 9,
                    tittle = "Missing Financial Reports in System",
                    description = "Some financial records for February 2025 are missing in the accounting system. The reports do not load correctly.",
                    status = "Pending",
                    priority = "High",
                    category = "Ergonomic Concerns",
                    reported_at = DateTime.Parse("2025-03-14 06:09:58"),
                    assigned_too = null,
                    token = "kHmRUB+JBdxmh0JVdEdynvQs7Sl/SupcCYCE/PYC+Tw=",
                    updated_at = null,
                    department_id = 16
                },
                new IncidentsModel
                {
                    incident_id = 17,
                    user_id = 3,
                    tittle = "System Downtime",
                    description = "The server suddenly shutdown and can't be opened.",
                    status = "In Progress",
                    priority = "High",
                    category = "Hardware Failures",
                    reported_at = DateTime.Parse("2025-03-15 10:27:52"),
                    assigned_too = 4,
                    token = "d5nLpbAe8s6k5RZY/UxjrdMHyYDbQp2eaq7zgQsl6ck=",
                    updated_at = null,
                    department_id = 12
                }
            );   
        }
    }
}
