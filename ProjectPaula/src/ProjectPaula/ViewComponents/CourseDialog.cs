using Microsoft.AspNetCore.Mvc;

namespace ProjectPaula.ViewComponents
{
    public class CourseDialog : ViewComponent
    {
        public IViewComponentResult Invoke() => View();
    }
}
