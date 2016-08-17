using Microsoft.AspNetCore.Mvc;

namespace ProjectPaula.ViewComponents
{
    public class CourseListModal : ViewComponent
    {
        public IViewComponentResult Invoke() => View();

    }
}
