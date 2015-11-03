using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPaul.Model
{
    public class Course
    {
        public string Id { get; set; }

        public string Name { get; set; }
        public string Docent { get; set; }

        public string Url { get; set; }
        public virtual List<Date> Dates { get; set; }

        public virtual List<Date> RegularDates { get; set; }
        public virtual List<Course> ConnectedCourses { get; set; }

        public virtual List<Tutorial> Tutorials { get; set; }
        public virtual CourseCatalogue Catalogue { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Course && (obj as Course).Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }


    public class Date
    {
        public long Id { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public string Room { get; set; }

        public string Instructor { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Date && ((Date)obj).From.Equals(From) && ((Date)obj).Instructor == Instructor && ((Date)obj).Room == Room && ((Date)obj).To.Equals(To);
        }
        public override int GetHashCode()
        {
            return Room.GetHashCode();
        }
    }

    class DateComparer : IEqualityComparer<Date>
    {
        public bool Equals(Date x, Date y)
        {
            return x.From.Hour == y.From.Hour && x.To.Hour == y.To.Hour && x.From.DayOfWeek == y.From.DayOfWeek;
        }

        public int GetHashCode(Date obj)
        {
            return obj.From.DayOfWeek.GetHashCode();
        }
    }
}
