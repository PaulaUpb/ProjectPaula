using Microsoft.AspNetCore.Mvc;

namespace ProjectPaula.ViewComponents
{
    public class ExitDialog : ViewComponent
    {
        public IViewComponentResult Invoke() => View();

    }
}
