using Microsoft.AspNetCore.Mvc;

namespace ProjectPaula.ViewComponents
{
    public class HelpDialog : ViewComponent
    {
        public IViewComponentResult Invoke() => View();
    }
}