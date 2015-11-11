using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectPaula.Model
{
    public class Schedule
    {
        public int Id { get; set; }

        public virtual List<User> User { get; set; }

        public virtual List<SelectedCourse> SelectedCourses { get; set; }

    }
}
