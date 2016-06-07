using Microsoft.AspNetCore.Mvc;

namespace ProjectPaula.ViewComponents
{
    public class ExportDialog : ViewComponent
    {
        public IViewComponentResult Invoke() => View();

    }
}
