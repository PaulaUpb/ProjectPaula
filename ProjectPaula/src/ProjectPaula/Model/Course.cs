using Microsoft.Data.Entity;
using Newtonsoft.Json;
using ProjectPaula.DAL;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPaula.Model
{
    [JsonObject(IsReference = true)]
    public class Course
    {
        public Course()
        {
            Dates = new List<Date>();
            Tutorials = new List<Course>();
            ConnectedCoursesInternal = new List<ConnectedCourse>();
            ShortName = "";
            Docent = "";
        }

        [JsonIgnore]
        public string Id { get; set; }

        public string Name { get; set; }

        public string ShortName { get; set; }

        public bool IsConnectedCourse { get; set; }

        public bool IsTutorial { get; set; }

        public string Docent { get; set; }
        [JsonIgnore]
        public string Url { get; set; }
        public virtual List<Date> Dates { get; set; }

        [NotMapped]
        public List<IGrouping<Date, Date>> RegularDates { get { return Dates?.GroupBy(d => d, new DateComparer()).ToList(); } }

        public virtual List<ConnectedCourse> ConnectedCoursesInternal { get; set; }

        [NotMapped]
        [JsonIgnore]
        public List<Course> ConnectedCourses
        {
            get
            {
                return PaulRepository.Courses.Where(c => ConnectedCoursesInternal.Any(con => con.CourseId2 == c.Id)).ToList();
            }
        }

        public virtual List<Course> Tutorials { get; set; }
        [JsonIgnore]
        public virtual CourseCatalog Catalogue { get; set; }

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
        [JsonIgnore]
        public long Id { get; set; }
        public DateTimeOffset From { get; set; }
        public DateTimeOffset To { get; set; }

        public string Room { get; set; }

        public string Instructor { get; set; }

        public virtual Course Course { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Date && ((Date)obj).From.Equals(From) && ((Date)obj).Instructor == Instructor && ((Date)obj).Room == Room && ((Date)obj).To.Equals(To);
        }
        public override int GetHashCode()
        {
            return From.DayOfWeek.GetHashCode();
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
