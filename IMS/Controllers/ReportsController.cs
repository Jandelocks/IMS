using IMS.Services;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Controllers
{
    public class ReportsController : ControllerBase
    {
        private readonly ReportService _reportService;

        public ReportsController(ReportService reportService)
        {
            _reportService = reportService;
        }

        //[HttpGet("incident/{incidentId}")]
        public IActionResult DownloadIncidentReport(int incidentId)
        {
            try
            {
                byte[] fileBytes = _reportService.GenerateIncidentReport(incidentId);

                return File(fileBytes, "application/pdf", $"IncidentReport_{incidentId}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
