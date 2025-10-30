using Microsoft.AspNetCore.Mvc;

namespace IMS.Controllers
{
    public class SingleSession : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
