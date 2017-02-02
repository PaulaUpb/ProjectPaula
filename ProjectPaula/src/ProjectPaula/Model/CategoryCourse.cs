using ProjectPaula.DAL;
using System.Linq;

namespace ProjectPaula.Model
{
    public class CategoryCourse
    {
        public string CourseId { get; set; }
        public int CategoryFilterId { get; set; }

        private Course _course;

        public Course Course => _course ?? (_course = PaulRepository.Courses.First(c => c.Id == CourseId));        

        public virtual CategoryFilter Category { get; set; }

    }
}
