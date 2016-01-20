using ProjectPaula.DAL;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectPaula.ViewModel
{
    public class CourseOverlapDetailViewModel
    {
        /// <summary>
        /// The name of the course for which overlaps are calculated.
        /// </summary>
        public string CourseName { get; }

        /// <summary>
        /// The names of all courses that overlap with the course.
        /// </summary>
        public string[] CourseNames { get; }

        /// <summary>
        /// The list of overlaps.
        /// Key: Formatted date and time string
        /// Value: An array of formatted time strings
        /// </summary>
        public List<KeyValuePair<string, string[]>> Overlaps { get; }

        /// <summary>
        /// Creates a table of overlaps of the following form:
        /// 
        /// Dates of Course| Course1 | Course2 | Course3
        /// ---------------+---------+---------+--------
        /// 10.11.15 9-11  | 9-11    | 10-12   | -
        /// 10.11.15 15-17 | 14-16   | -       | 14-16
        /// 18.11.15 9-13  | 11-13   | 11-13   | 12-14
        /// ...            | ...     | ...     | ...
        /// </summary>
        /// <param name="scheduleVM">Schedule VM</param>
        /// <param name="courseId">Course ID</param>
        public CourseOverlapDetailViewModel(ScheduleViewModel scheduleVM, string courseId)
        {
            var course = PaulRepository.Courses.Find(c => c.Id == courseId);

            if (course == null)
            {
                throw new ArgumentException("Course not found", nameof(courseId));
            }

            CourseName = course.Name;

            var overlappingCourses = scheduleVM.OverlappingDates
                .Where(group => group.Key.Course.Id == courseId)
                .SelectMany(dates => dates.Value.Select(d => d.Course))
                .Except(new[] { course })
                .Distinct()
                .OrderBy(c => c.Name)
                .ToArray();

            // Assumption: There exist no courses with the same names
            CourseNames = overlappingCourses.Select(c => c.Name).ToArray();

            Overlaps = course.Dates.OrderBy(d => d.From).Select(date =>
                new KeyValuePair<string, string[]>(
                    date.FormattedDateTimeString,
                    overlappingCourses
                        .Select(c => string.Join(", ", c.Dates.Where(date.Intersects).Select(d => d.FormattedTimeString)))
                        .ToArray())).ToList();
        }
    }
}
