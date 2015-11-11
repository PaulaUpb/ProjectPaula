using System.Collections.Generic;

namespace ProjectPaula.Model
{
    public class SelectedCourse
    {
        public int Id { get; set; }
        public virtual Course Course { get; set; }
        public virtual List<SelectedCourseUser> Users { get; set; }
    }

}