using Newtonsoft.Json;
using ProjectPaula.DAL;
using System.Collections.Generic;
using System.Linq;

namespace ProjectPaula.Model
{
    [JsonObject(IsReference = true)]
    public class SelectedCourse
    {
        public int Id { get; set; }
        public string CourseId { get; set; }

        public string ScheduleId { get; set; }

        private Course _course;

        public Course Course => _course ?? (_course = PaulRepository.Courses.First(c => c.Id == CourseId));
        public virtual List<SelectedCourseUser> Users { get; set; }
    }

}