using Microsoft.AspNet.Mvc;

namespace ProjectPaula.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();

        public IActionResult Chat() => View();

        public IActionResult Error() => View("~/Views/Shared/Error.cshtml");
    }
}
