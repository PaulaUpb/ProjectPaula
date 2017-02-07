using System.Collections.Generic;
using System.Linq;

namespace ProjectPaula.Model.Public
{
    public class PublicCourse
    {
        private readonly Course _course;
        private readonly List<Course> _allCourses;

        /// <summary>
        /// URL to PAUL. Needs to be prefixed with the PAUL base URL.
        /// </summary>
        public string Url => _course.Url;

        /// <summary>
        /// The PAUL ID, something like K.184.32843. NULL in case IsTutorial == true.
        /// </summary>
        public string PaulId => _course.InternalCourseID;

        /// <summary>
        /// The ID that will be referenced by TutorialIds and ConnectedCourseIds.
        /// </summary>
        public string Id => _course.Id;

        /// <summary>
        /// The name of the course, for example "Analysis", "K.184.32843", or anything really.
        /// </summary>
        public string Name => _course.Name;

        /// <summary>
        /// The short name of the course, for example "ANA", "Analysis", "K.184.32843", or anything at all again.
        /// </summary>
        public string ShortName => _course.ShortName;

        /// <summary>
        /// See doc for property ConnectedCourseIds. This property is true
        /// if this is a course connected to some other course, but not if
        /// this course owns connected courses.
        /// </summary>
        public bool IsConnectedCourse => _course.IsConnectedCourse;

        /// <summary>
        /// True in case this is a tutorial to another course.
        /// </summary>
        public bool IsTutorial => _course.IsTutorial;

        /// <summary>
        /// In case IsTutorial == true, this contains the ID of the parent course.
        /// </summary>
        public string ParentCourseId => _course.FindParent(_allCourses)?.InternalCourseID;

        /// <summary>
        /// A list of dates of this course.
        /// </summary>
        public List<PublicDate> Dates => _course.Dates.Select(date => new PublicDate(date)).ToList();

        /// <summary>
        /// Exam dates.
        /// </summary>
        public List<PublicDate> ExamDates => _course.ExamDates.Select(date => new PublicDate(date)).ToList();

        /// <summary>
        /// Docent.
        /// </summary>
        public string Docent => _course.Docent;

        /// <summary>
        /// In case this is a parent to one or more tutorials, this is a list of course IDs to courses
        /// that are tutorials belonging to this. These IDs are not PAUL IDS!
        /// </summary>
        public List<string> TutorialIds => _course.Tutorials.Select(c => c.Id).ToList();

        /// <summary>
        /// Not all tutorials are modeled as Tutorial courses in PAUL. Courses can also be connected,
        /// so docents use this to create two separate courses, one being for lectures and the other containing the tutorials
        /// (meaning that TutorialsId is non-empty for the connected course). A connected course does not have
        /// a reference to THIS course in its connectedCourseIds to avoid loops.
        /// </summary>
        public List<string> ConnectedCourseIds => _course.ConnectedCourses.Select(c => c.Id).ToList();

        public PublicCourse(Course course, List<Course> allCourses)
        {
            _allCourses = allCourses;
            _course = course;
        }
    }
}
