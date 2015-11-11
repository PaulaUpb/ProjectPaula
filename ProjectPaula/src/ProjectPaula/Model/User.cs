using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectPaula.Model
{
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public virtual List<SelectedCourseUser> SelectedCourses { get; set; }
    }
}
