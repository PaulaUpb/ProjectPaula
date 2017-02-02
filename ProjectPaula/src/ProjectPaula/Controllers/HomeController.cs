using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectPaula.Model.CalendarExport;

namespace ProjectPaula.Controllers
{
    public class HomeController : Controller
    {
        public HomeController(IHttpContextAccessor accessor)
        {
            UrlHelper.Configure(accessor);
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Error() => View("~/Views/Shared/Error.cshtml");

        public IActionResult Impressum() => View();

    }
}
