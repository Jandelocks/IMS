using System;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using IMS.Data;
using IMS.Models;

namespace IMS.Services
{
    public class ReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public byte[] GenerateIncidentReport(int incidentId)
        {
            var incident = _context.Incidents
                .Where(i => i.incident_id == incidentId)
                .Select(i => new
                {
                    i.incident_id,
                    i.tittle,
                    i.description,
                    i.status,
                    i.priority,
                    i.reported_at,
                    UserFullName = i.User.full_name, // Reported by
                    ModeratorFullName = _context.Users
                        .Where(m => m.user_id == i.assigned_too)
                        .Select(m => m.full_name)
                        .FirstOrDefault(), // Assigned Moderator
                    ModeratorDepartment = _context.Users
                        .Where(m => m.user_id == i.assigned_too)
                        .Select(m => m.department)
                        .FirstOrDefault(), // Moderator's Department
                    Updates = _context.Updates.Where(u => u.incident_id == incidentId)
                        .Select(u => new { u.update_text, u.updated_at }).ToList(),
                    Comments = _context.Comments.Where(c => c.incident_id == incidentId)
                        .Select(c => new { c.comment_text, c.commented_at, c.rating, User = c.User.full_name })
                        .ToList() // Fetching user feedback with rating
                })
                .FirstOrDefault();

            if (incident == null)
            {
                throw new Exception("Incident not found.");
            }

            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                document.Open();

                Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                Font normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                Font subHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);

                document.Add(new Paragraph("Incident Report", titleFont));
                document.Add(new Paragraph($"Incident ID: {incident.incident_id}", normalFont));
                document.Add(new Paragraph($"Title: {incident.tittle}", normalFont));
                document.Add(new Paragraph($"Description: {incident.description}", normalFont));
                document.Add(new Paragraph($"Status: {incident.status}", normalFont));
                document.Add(new Paragraph($"Priority: {incident.priority}", normalFont));
                document.Add(new Paragraph($"Reported By: {incident.UserFullName}", normalFont));
                document.Add(new Paragraph($"Reported At: {incident.reported_at.ToString("MMMM dd, yyyy, hh:mm tt")}", normalFont));
                document.Add(new Paragraph("\n"));

                // Moderator Details
                document.Add(new Paragraph("Moderator Information", subHeaderFont));
                if (!string.IsNullOrEmpty(incident.ModeratorFullName))
                {
                    document.Add(new Paragraph($"Assigned Moderator: {incident.ModeratorFullName}", normalFont));
                    document.Add(new Paragraph($"Moderator Department: {incident.ModeratorDepartment}", normalFont));
                }
                else
                {
                    document.Add(new Paragraph("Assigned Moderator: Not Assigned", normalFont));
                }
                document.Add(new Paragraph("\n"));

                // Updates Section
                document.Add(new Paragraph("Updates", subHeaderFont));
                if (incident.Updates.Any())
                {
                    foreach (var update in incident.Updates)
                    {
                        document.Add(new Paragraph($"Comment: {update.update_text}", normalFont));
                        document.Add(new Paragraph($"Date: {update.updated_at?.ToString("MMMM dd, yyyy, hh:mm tt")}", normalFont));
                        document.Add(new Paragraph("\n")); // Space between updates
                    }
                }
                else
                {
                    document.Add(new Paragraph("No updates available.", normalFont));
                }
                document.Add(new Paragraph("\n"));

                // User Feedback Section with Ratings
                document.Add(new Paragraph("User Feedback & Ratings", subHeaderFont));
                if (incident.Comments.Any())
                {
                    foreach (var comment in incident.Comments)
                    {
                        document.Add(new Paragraph($"User: {comment.User}", normalFont));
                        document.Add(new Paragraph($"Feedback: {comment.comment_text}", normalFont));
                        document.Add(new Paragraph($"Rating: {comment.rating} / 5", normalFont)); // ⭐ Added Rating
                        document.Add(new Paragraph($"Date: {comment.commented_at.ToString("MMMM dd, yyyy, hh:mm tt")}", normalFont));
                        document.Add(new Paragraph("\n")); // Space between feedbacks
                    }
                }
                else
                {
                    document.Add(new Paragraph("No user feedback available.", normalFont));
                }

                document.Close();
                return ms.ToArray();
            }
        }
    }
}
