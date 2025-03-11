using Microsoft.AspNetCore.Mvc;

namespace IMS.Controllers
{
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
