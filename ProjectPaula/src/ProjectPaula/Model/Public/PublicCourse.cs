using System.Collections.Generic;
using System.Linq;

namespace ProjectPaula.Model.Public
{
    public class PublicCourse
    {
        private readonly Course Course;
        private readonly List<Course> AllCourses;

        /// <summary>
        /// URL to PAUL. Needs to be prefixed with the PAUL base URL.
        /// </summary>
        public string Url => Course.Url;

        /// <summary>
        /// The PAUL ID, something like K.184.32843. NULL in case IsTutorial == true.
        /// </summary>
        public string PaulId => Course.InternalCourseID;

        /// <summary>
        /// The ID that will be referenced by TutorialIds and ConnectedCourseIds.
        /// </summary>
        public string Id => Course.Id;

        /// <summary>
        /// The name of the course, for example "Analysis", "K.184.32843", or anything really.
        /// </summary>
        public string Name => Course.Name;

        /// <summary>
        /// The short name of the course, for example "ANA", "Analysis", "K.184.32843", or anything at all again.
        /// </summary>
        public string ShortName => Course.ShortName;

        /// <summary>
        /// TODO
        /// </summary>
        public bool IsConnectedCourse => Course.IsConnectedCourse;

        /// <summary>
        /// True in case this is a tutorial to another course.
        /// </summary>
        public bool IsTutorial => Course.IsTutorial;

        /// <summary>
        /// In case IsTutorial == true, this contains the ID of the parent course.
        /// </summary>
        public string ParentCourseId => Course.FindParent(AllCourses)?.InternalCourseID;

        /// <summary>
        /// A list of dates of this course.
        /// </summary>
        public List<PublicDate> Dates => Course.Dates.Select(date => new PublicDate(date)).ToList();

        /// <summary>
        /// Exam dates.
        /// </summary>
        public List<PublicDate> ExamDates => Course.ExamDates.Select(date => new PublicDate(date)).ToList();

        /// <summary>
        /// Docent.
        /// </summary>
        public string Docent => Course.Docent;

        /// <summary>
        /// In case this is a parent to one or more tutorials, this is a list of course IDs to courses
        /// that are tutorials belonging to this. These IDs are not PAUL IDS!
        /// </summary>
        public List<string> TutorialIds => Course.Tutorials.Select(c => c.Id).ToList();

        /// <summary>
        /// Not all tutorials are modeled as Tutorial courses in PAUL. Courses can also be connected,
        /// so docents use this to create two separate courses, one being for lectures and the other containing the tutorials
        /// (meaning that TutorialsId is non-empty for the connected course). A connected course does not have
        /// a reference to THIS course in its connectedCourseIds to avoid loops.
        /// </summary>
        public List<string> connectedCourseIds => Course.ConnectedCourses.Select(c => c.Id).ToList();

        public PublicCourse(Course course, List<Course> allCourses)
        {
            AllCourses = allCourses;
            Course = course;
        }
    }
}
