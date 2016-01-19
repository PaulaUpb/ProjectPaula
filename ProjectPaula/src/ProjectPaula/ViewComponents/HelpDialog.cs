using Microsoft.AspNet.Mvc;

namespace ProjectPaula.ViewComponents
{
    public class HelpDialog : ViewComponent
    {
        public IViewComponentResult Invoke() => View();
    }
}