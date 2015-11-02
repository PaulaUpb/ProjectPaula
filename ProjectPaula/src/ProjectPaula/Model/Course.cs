using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaulParserDesktop
{
    class Course
    {
        public string Name { get; set; }
        public string Docent { get; set; }

        public string Url { get; set; }
    }

    class CourseDetail
    {
        public List<Date> Dates { get; set; }

        public List<Date> RegularDates { get; set; }
        public List<Course> ConnectedCourses { get; }

        public List<Tutorial> Tutorials { get; set; }


        public CourseDetail()
        {
            Dates = new List<Date>();
            ConnectedCourses = new List<Course>();
            Tutorials = new List<Tutorial>();
        }
    }

    class Date
    {
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
