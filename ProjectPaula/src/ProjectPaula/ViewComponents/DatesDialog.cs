using Microsoft.AspNetCore.Mvc;

namespace ProjectPaula.ViewComponents
{
    public class DatesDialog : ViewComponent
    {
        public IViewComponentResult Invoke() => View();
    }
}