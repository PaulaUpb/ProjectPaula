using Microsoft.AspNet.Mvc;
using ProjectPaula.DAL;

namespace ProjectPaula.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();

        public IActionResult Chat() => View();

        public IActionResult Error() => View("~/Views/Shared/Error.cshtml");

        public IActionResult Impressum() => View();
        
    }
}
