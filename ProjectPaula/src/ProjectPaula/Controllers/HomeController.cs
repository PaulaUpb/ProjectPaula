using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectPaula.Model.CalendarExport;

namespace ProjectPaula.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            UrlHelper.Initialize(HttpContext);
            return View();
        }

        public IActionResult Error() => View("~/Views/Shared/Error.cshtml");

        public IActionResult Impressum() => View();

    }
}
