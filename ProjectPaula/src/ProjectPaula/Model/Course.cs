using Newtonsoft.Json;
using ProjectPaula.DAL;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Net;

namespace ProjectPaula.Model
{
    [JsonObject(IsReference = true)]
    public class Course
    {
        public string Url { get; private set; }

        public Course()
        {
            Dates = new List<Date>();
            ExamDates = new List<ExamDate>();
            Tutorials = new List<Course>();
            ConnectedCoursesInternal = new List<ConnectedCourse>();
            ShortName = "";
            Docent = "";
        }


        //Column TypeName required so that EF doesn't do an implicit conversion from nvarchar to varchar
        [JsonIgnore]
        public string Id
        {
            get { return Id; }
            set { Id = value.Replace("'", ""); }
        }

        /// <summary>
        /// Id of the parent course (only set if course is a tutorial)
        /// </summary>
        public string CourseId { get; set; }

        /// <summary>
        /// The ID that represents the course in PAUL. NOTE TO CHRISTIAN FROM CHRISTIAN: Null in case this is a tutorial.
        /// </summary>
        public string InternalCourseID { get; set; }

        public string Name { get; set; }

        public string ShortName { get; set; }

        public bool IsConnectedCourse { get; set; }

        public bool IsTutorial { get; set; }

        [NotMapped]
        public bool DatesChanged { get; set; }

        public string Docent { get; set; }

        /// <summary>
        /// The partial URL (without the base url) to the course.
        /// </summary>
        /// <remarks>
        /// We use HtmlDecode(...) to decode things like "&amp;" that may
        /// occurr in the URL. The getter ensures that we get the right URL.
        /// The setter ensures that any new data that is saved is already
        /// correctly decoded in the database. This way we can remove the
        /// decoding code after 1 year (2 semesters).
        /// </remarks>
        [JsonIgnore]
        [NotMapped]
        public string TrimmedUrl
        {
            get { return WebUtility.HtmlDecode(Url); }
            set { Url = WebUtility.HtmlDecode(value); }
        }

        [JsonIgnore]
        [NotMapped]
        private string _newUrl;
        /// <summary>
        /// In case the web request fails, this property stores the "new" url
        /// </summary>
        /// 
        [NotMapped]
        public string NewUrl
        {
            get { return _newUrl; }
            set { _newUrl = value; }
        }

        public virtual List<Date> Dates { get; set; }

        public virtual List<ExamDate> ExamDates { get; set; }

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
        public List<Course> ParsedConnectedCourses => _parsedConnectedCourses ??
                                                      (_parsedConnectedCourses =
                                                          PaulRepository.Courses.Where(c => ConnectedCoursesInternal.Any(con => con.CourseId2 == c.Id))
                                                              .ToList());

        [NotMapped]
        [JsonIgnore]
        private List<List<Course>> _allTutorials;

        /// <summary>
        /// All tutorials belonging to this course, including connected courses.
        /// The inner list describes a group of tutorials.
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public List<List<Course>> AllTutorials => _allTutorials = _allTutorials ?? new List<List<Course>>() { Tutorials }
            .Concat(ConnectedCourses
                .Where(connectedCourse => !connectedCourse.IsTutorial)
                .Select(connectedCourse => connectedCourse.Tutorials))
            .ToList();

        public virtual List<Course> Tutorials { get; set; }

        [NotMapped]
        [JsonIgnore]
        private List<Course> _parsedTutorials;
        [NotMapped]
        [JsonIgnore]
        public List<Course> ParsedTutorials => _parsedTutorials ?? (_parsedTutorials = new List<Course>(Tutorials));

        private Course _parent;
        public Course FindParent(IEnumerable<Course> parentCandidates) => _parent = _parent ??
            parentCandidates.FirstOrDefault(candidate => candidate.AllTutorials.SelectMany(it => it).Contains(this));

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

        public virtual List<CategoryCourse> Categories { get; set; }
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

        public string CourseId { get; set; }
        public virtual Course Course { get; set; }

        private sealed class StructuralEqualityComparer : IEqualityComparer<Date>
        {
            public bool Equals(Date x, Date y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.From.Equals(y.From) && x.To.Equals(y.To) && string.Equals(x.Room, y.Room) && string.Equals(x.Instructor, y.Instructor);
            }

            public int GetHashCode(Date obj)
            {
                unchecked
                {
                    var hashCode = obj.From.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.To.GetHashCode();
                    hashCode = (hashCode * 397) ^ (obj.Room?.GetHashCode() ?? 0);
                    hashCode = (hashCode * 397) ^ (obj.Instructor?.GetHashCode() ?? 0);
                    return hashCode;
                }
            }
        }

        /// <summary>
        /// <see cref="StructuralEquals"/>
        /// </summary>s
        public static IEqualityComparer<Date> StructuralComparer { get; } = new StructuralEqualityComparer();

        public override bool Equals(object obj)
        {
            Date other = obj as Date;

            if (other != null && other.GetType() == typeof(Date))
            {
                return
                    From.EqualsExact(other.From) &&
                    To.EqualsExact(other.To) &&
                    Instructor == other.Instructor &&
                    Room == other.Room &&
                    CourseId == other.CourseId;
            }

            return false;
        }

        /// <summary>
        /// Returns true iff From, To, Instructor and Room are equal.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool StructuralEquals(object obj)
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
            var hashCode = From.GetHashCode();
            hashCode = (hashCode * 397) ^ To.GetHashCode();
            hashCode = (hashCode * 397) ^ (Room?.GetHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ (Instructor?.GetHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ (CourseId?.GetHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ (GetType()?.GetHashCode() ?? 0);
            return hashCode;
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
        /// Check if the two dates are starting and ending at the same time on the same day of the week.
        /// </summary>
        /// <param name="sameCourse">If set to true, both dates must belong to the same course</param>
        public static bool SameGroup(Date x, Date y, bool sameCourse)
        {
            // !! If you update this, also update DateComparer.GetHashCode !!
            return (!sameCourse || x.Course.Id == y.Course.Id)
                   && x.From.Hour == y.From.Hour
                   && x.From.Minute == y.From.Minute
                   && x.From.DayOfWeek == y.From.DayOfWeek
                   && x.To.Hour == y.To.Hour
                   && x.To.Minute == y.To.Minute
                   && x.To.DayOfWeek == y.To.DayOfWeek;
        }
    }

    public class ExamDate
    {
        public string Description { get; set; }

        [JsonIgnore]
        public long Id { get; set; }
        public DateTimeOffset From { get; set; }
        public DateTimeOffset To { get; set; }

        public string Instructor { get; set; }

        public string CourseId { get; set; }
        public virtual Course Course { get; set; }

        public override bool Equals(object obj)
        {
            ExamDate other = obj as ExamDate;

            if (other != null)
            {
                return
                    From.EqualsExact(other.From) &&
                    To.EqualsExact(other.To) &&
                    Instructor == other.Instructor &&
                    Description == other.Description &&
                    CourseId == other.CourseId;
            }

            return false;

        }

        public override int GetHashCode()
        {
            var hashCode = From.GetHashCode();
            hashCode = (hashCode * 397) ^ To.GetHashCode();
            hashCode = (hashCode * 397) ^ (Description?.GetHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ (Instructor?.GetHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ (CourseId?.GetHashCode() ?? 0);
            hashCode = (hashCode * 397) ^ (GetType()?.GetHashCode() ?? 0);
            return hashCode;
        }
    }

    class DateComparer : IEqualityComparer<Date>
    {
        public bool Equals(Date x, Date y) => Date.SameGroup(x, y, false);

        public int GetHashCode(Date obj)
        {
            var hashCode = obj.From.Hour.GetHashCode();
            hashCode = (hashCode * 397) ^ obj.From.Minute.GetHashCode();
            hashCode = (hashCode * 397) ^ obj.From.DayOfWeek.GetHashCode();
            hashCode = (hashCode * 397) ^ obj.To.Minute.GetHashCode();
            hashCode = (hashCode * 397) ^ obj.To.Minute.GetHashCode();
            hashCode = (hashCode * 397) ^ obj.To.DayOfWeek.GetHashCode();

            return hashCode;
        }
    }
}
