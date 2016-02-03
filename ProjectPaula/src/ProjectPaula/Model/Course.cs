using Newtonsoft.Json;
using ProjectPaula.DAL;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;

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


        //Column TypeName required so that EF doesn't do an implicit conversion from nvarchar to varchar
        [JsonIgnore]
        public string Id { get; set; }

        /// <summary>
        /// Id of the parent course (only set if course is a tutorial)
        /// </summary>
        public string CourseId { get; set; }

        public string InternalCourseID { get; set; }

        public string Name { get; set; }

        public string ShortName { get; set; }

        public bool IsConnectedCourse { get; set; }

        public bool IsTutorial { get; set; }

        [NotMapped]
        public bool DatesChanged { get; set; }

        public string Docent { get; set; }
        [JsonIgnore]
        public string Url { get; set; }
        public virtual List<Date> Dates { get; set; }

        [NotMapped]
        public List<IGrouping<Date, Date>> RegularDates { get { return Dates?.GroupBy(d => d, new DateComparer()).ToList(); } }

        public virtual List<ConnectedCourse> ConnectedCoursesInternal { get; set; }

        [NotMapped]
        [JsonIgnore]
        private List<Course> _connectedCourses;

        [NotMapped]
        [JsonIgnore]
        public List<Course> ConnectedCourses => _connectedCourses = _connectedCourses ??
            PaulRepository.Courses.Where(c => ConnectedCoursesInternal.Any(con => con.CourseId2 == c.Id)).ToList();


        private List<Course> _parsedConnectedCourses;
        /// <summary>
        /// This property is needed for parsing, because it needs the current connected courses (including courses added by parsing) not the cached one
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public List<Course> ParsedConnectedCourses
        {
            get
            {
                if (_parsedConnectedCourses == null) _parsedConnectedCourses = PaulRepository.Courses.Where(c => ConnectedCoursesInternal.Any(con => con.CourseId2 == c.Id)).ToList();
                return _parsedConnectedCourses;
            }
        }

        [NotMapped]
        [JsonIgnore]
        private List<Course> _allTutorials;

        /// <summary>
        /// All tutorials belonging to this course, including connected courses.
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public List<Course> AllTutorials => _allTutorials = _allTutorials ?? Tutorials
            .Concat(ConnectedCourses
                .Where(connectedCourse => !connectedCourse.IsTutorial)
                .SelectMany(connectedCourse => connectedCourse.Tutorials))
            .ToList();

        public virtual List<Course> Tutorials { get; set; }

        [NotMapped]
        [JsonIgnore]
        private List<Course> _parsedTutorials;
        [NotMapped]
        [JsonIgnore]
        public List<Course> ParsedTutorials
        {
            get
            {
                if (_parsedTutorials == null) _parsedTutorials = new List<Course>(Tutorials);
                return _parsedTutorials;
            }
        }

        private Course _parent;
        public Course FindParent(IEnumerable<Course> parentCandidates) => _parent = _parent ??
            parentCandidates.FirstOrDefault(candidate => candidate.AllTutorials.Contains(this));

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

        public override string ToString() => Name;
    }



    public class Date
    {
        [JsonIgnore]
        public long Id { get; set; }
        public DateTimeOffset From { get; set; }
        public DateTimeOffset To { get; set; }

        [NotMapped]
        public string FormattedTimeString => $"{From.ToString("HH:mm", new CultureInfo("de-DE"))} - {To.ToString("HH:mm", new CultureInfo("de-DE"))}";

        [NotMapped]
        public string FormattedDateTimeString => $"{From.ToString("ddd. dd.MM.yy, HH:mm", new CultureInfo("de-DE"))} - {To.ToString("HH:mm", new CultureInfo("de-DE"))}";

        public string Room { get; set; }

        public string Instructor { get; set; }

        public virtual Course Course { get; set; }

        public override bool Equals(object obj)
        {
            Date other = obj as Date;

            if (other != null)
            {
                return
                    From.EqualsExact(other.From) &&
                    To.EqualsExact(other.To) &&
                    Instructor == other.Instructor &&
                    Room == other.Room;
            }

            return false;
        }
        public override int GetHashCode()
        {
            return From.DayOfWeek.GetHashCode();
        }

        public override string ToString()
        {
            return $"From: {From}, Course: {Course}";
        }

        /// <summary>
        /// Checks whether the date/time ranges of this
        /// date and the specified second date overlap.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Intersects(Date other)
        {
            return
                (other.From >= From && other.From < To) ||
                (From >= other.From && From < other.To);
        }

        /// <summary>
        /// Check if the two dates are starting at the same time on the same day of the week.
        /// </summary>
        /// <param name="sameCourse">If set to true, both dates must belong to the same course</param>
        public static bool SameGroup(Date x, Date y, bool sameCourse) => (!sameCourse || x.Course.Id == y.Course.Id) && x.From.Hour == y.From.Hour
            && x.From.Minute == y.From.Minute && x.From.DayOfWeek == y.From.DayOfWeek;
    }

    class DateComparer : IEqualityComparer<Date>
    {
        public bool Equals(Date x, Date y) => Date.SameGroup(x, y, false);

        public int GetHashCode(Date obj)
        {
            return obj.From.DayOfWeek.GetHashCode();
        }
    }
}
