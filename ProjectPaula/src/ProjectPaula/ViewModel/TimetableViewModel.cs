using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProjectPaula.Model;

namespace ProjectPaula.ViewModel
{
    public class TimetableViewModel
    {

        private static readonly List<DayOfWeek> DaysOfWeek = new List<DayOfWeek>() {DayOfWeek.Monday, DayOfWeek.Tuesday,
            DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday};

        public List<string> HalfHourTimes;
        public List<Weekday> Weekdays;

        public static TimetableViewModel CreateFrom(Timetable timetable)
        {
            var coursesForDay = new Dictionary<DayOfWeek, List<ViewModelMultiCourse>>();
            foreach (var dayOfWeek in DaysOfWeek)
            {
                for (var halfHourTime = 0; halfHourTime < timetable.HalfHourCount;)
                {
                    if (!coursesForDay.ContainsKey(dayOfWeek))
                    {
                        coursesForDay[dayOfWeek] = new List<ViewModelMultiCourse>();
                    }

                    var multiCourse = GetCoursesAt(timetable, dayOfWeek, halfHourTime);
                    if (multiCourse != null)
                    {

                        coursesForDay[dayOfWeek].Add(multiCourse);
                        halfHourTime += multiCourse.LengthInHalfHours.Value;
                    }
                    else
                    {
                        coursesForDay[dayOfWeek].Add(new ViewModelMultiCourse(null, empty: true));
                        halfHourTime++;
                    }

                }

            }

            var weekdays = new[]
            {
                new Weekday(DayOfWeek.Monday, "Monday", coursesForDay[DayOfWeek.Monday]),
                new Weekday(DayOfWeek.Tuesday, "Tuesday", coursesForDay[DayOfWeek.Tuesday]),
                new Weekday(DayOfWeek.Wednesday, "Wednesday", coursesForDay[DayOfWeek.Wednesday]),
                new Weekday(DayOfWeek.Thursday, "Thursday", coursesForDay[DayOfWeek.Thursday]),
                new Weekday(DayOfWeek.Friday, "Friday", coursesForDay[DayOfWeek.Friday]),
                new Weekday(DayOfWeek.Saturday, "Saturday", coursesForDay[DayOfWeek.Saturday]),
                new Weekday(DayOfWeek.Sunday, "Sunday", coursesForDay[DayOfWeek.Sunday]),
            };

            return new TimetableViewModel
            {
                HalfHourTimes = timetable.HalfHourTimes().Select(it => it.ToString("HH:mm")).ToList(),
                Weekdays = weekdays.ToList()
            };
        }

        private static ViewModelMultiCourse GetCoursesAt(Timetable timetable, DayOfWeek dayOfWeek, int halfHour)
        {
            var timeToFind = timetable.EarliestTime.AddMinutes(30 * halfHour);

            if (!timetable.CoursesByDay.ContainsKey(dayOfWeek))
            {
                return null;
            }

            var courses = timetable.CoursesByDay[dayOfWeek];
            var startingCourse = courses.Find(course => course.Begin.Hour == timeToFind.Hour && course.Begin.Minute == timeToFind.Minute);
            if (startingCourse == null)
            {
                return null;
            }

            // We've found a matching course, now find overlapping courses
            var coursesInFoundCourseInterval = new List<ViewModelCourse> { ConvertToViewModelCourse(startingCourse) };
            for (var i = 1; i < halfHour + startingCourse.LengthInHalfHours; i++)
            {
                var overlappingTimeToFind = timeToFind.AddMinutes(i * 30);
                var overlappingCourse =
                    courses.Find(
                        course => course.Begin.Hour == overlappingTimeToFind.Hour && course.Begin.Minute == overlappingTimeToFind.Minute);
                if (overlappingCourse != null)
                {
                    coursesInFoundCourseInterval.Add(ConvertToViewModelCourse(overlappingCourse));
                }
            }

            return new ViewModelMultiCourse(coursesInFoundCourseInterval, false);
        }

        private static ViewModelCourse ConvertToViewModelCourse(Model.MockCourse course)
        {
            return new ViewModelCourse(course.Title, course.Begin, course.End);
        }


        public class Weekday
        {
            public DayOfWeek DayOfWeek { get; }
            public string Description { get; }
            public List<ViewModelMultiCourse> MultiCourses { get; }

            public Weekday(DayOfWeek dayOfWeek, string description, List<ViewModelMultiCourse> multiCourses)
            {
                DayOfWeek = dayOfWeek;
                Description = description;
                MultiCourses = multiCourses;
            }
        }

        public class ViewModelMultiCourse
        {
            public List<ViewModelCourse> Courses { get; }

            public DateTime? Begin => Courses?.Select(c => c.Begin).Min();

            public DateTime? End => Courses?.Select(c => c.End).Max();

            public int? LengthInHalfHours => End != null && Begin != null ? ((int)(End - Begin).Value.TotalMinutes) / 30 : (int?) null;
            public bool Empty { get; }

            public ViewModelMultiCourse(List<ViewModelCourse> courses, bool empty)
            {
                Courses = courses;
                Empty = empty;

                if (courses != null)
                {
                    foreach (var viewModelCourse in courses)
                    {
                        viewModelCourse.HalfHourOffset = ((int)(viewModelCourse.Begin - Begin).Value.TotalMinutes) / 30;
                    }
                }
            }
        }


        public class ViewModelCourse
        {
            public string Title { get; }

            public DateTime Begin { get; }

            public DateTime End { get; }
            public int HalfHourOffset { get; set; }

            public int LengthInHalfHours => ((int)(End - Begin).TotalMinutes) / 30;

            public ViewModelCourse(string title, DateTime begin, DateTime end)
            {
                Title = title;
                Begin = begin;
                End = end;
            }
        }


    }
}
