using Microsoft.AspNetCore.Mvc;

namespace ProjectPaula.ViewComponents
{
    public class ShareDialog : ViewComponent
    {
        public IViewComponentResult Invoke() => View();

    }
}
