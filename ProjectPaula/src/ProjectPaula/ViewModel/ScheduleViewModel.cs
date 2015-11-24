using System;
using System.Collections.Generic;
using System.Linq;
using ProjectPaula.Model;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;
using ProjectPaula.Model.ObjectSynchronization;

namespace ProjectPaula.ViewModel
{
    public class ScheduleViewModel : BindableBase
    {
        /// <summary>
        /// Enumeration of all days of the week in the order they appear
        /// in the calender, starting with Monday.
        /// </summary>
        private static readonly List<DayOfWeek> DaysOfWeek = new List<DayOfWeek>()
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
            DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
        };

        private ObservableCollection<Weekday> _weekdays;

        /// <summary>
        /// EarliestTime, ..., 15:00, 15:30, ..., LatestTime
        /// </summary>
        public ObservableCollection<string> HalfHourTimes { get; } = new ObservableCollection<string>();

        /// <summary>
        /// A collection of Weekdays containing the data about courses.
        /// </summary>
        public ObservableCollection<Weekday> Weekdays
        {
            get { return _weekdays; }
            private set { Set(ref _weekdays, value); }
        }

        /// <summary>
        /// Update this ViewModel to match the data in the schedule.
        /// </summary>
        /// <param name="schedule"></param>
        public void UpdateFrom(Schedule schedule)
        {
            var coursesForDay = new Dictionary<DayOfWeek, List<MultiCourseViewModel>>();
            var selectedCoursesByCourse = schedule.SelectedCourses.ToDictionary(selectedCourse => selectedCourse.Course);
            foreach (var dayOfWeek in DaysOfWeek)
            {
                if (!coursesForDay.ContainsKey(dayOfWeek))
                {
                    coursesForDay[dayOfWeek] = new List<MultiCourseViewModel>();
                }

                for (var halfHourTime = 0; halfHourTime < schedule.HalfHourCount;)
                {
                    var multiCourse = GetCoursesAt(schedule, selectedCoursesByCourse, dayOfWeek, halfHourTime);
                    if (multiCourse != null)
                    {

                        coursesForDay[dayOfWeek].Add(multiCourse);
                        halfHourTime += multiCourse.LengthInHalfHours.Value;
                    }
                    else
                    {
                        coursesForDay[dayOfWeek].Add(new MultiCourseViewModel());
                        halfHourTime++;
                    }

                }

            }

            var weekdays = new List<Weekday>
            {
                new Weekday(DayOfWeek.Monday, "Monday", coursesForDay[DayOfWeek.Monday]),
                new Weekday(DayOfWeek.Tuesday, "Tuesday", coursesForDay[DayOfWeek.Tuesday]),
                new Weekday(DayOfWeek.Wednesday, "Wednesday", coursesForDay[DayOfWeek.Wednesday]),
                new Weekday(DayOfWeek.Thursday, "Thursday", coursesForDay[DayOfWeek.Thursday]),
                new Weekday(DayOfWeek.Friday, "Friday", coursesForDay[DayOfWeek.Friday]),
            };
            if (coursesForDay[DayOfWeek.Saturday].Any(x => !x.Empty) || coursesForDay[DayOfWeek.Sunday].Any(x => !x.Empty))
            {
                weekdays.Add(new Weekday(DayOfWeek.Saturday, "Saturday", coursesForDay[DayOfWeek.Saturday]));
            }
            if (coursesForDay[DayOfWeek.Sunday].Any(x => !x.Empty))
            {
                weekdays.Add(new Weekday(DayOfWeek.Sunday, "Sunday", coursesForDay[DayOfWeek.Sunday]));
            }

            HalfHourTimes.Clear();
            HalfHourTimes.AddRange(schedule.HalfHourTimes.Select(it => it.ToString("HH:mm")).ToList());
            Weekdays = new ObservableCollection<Weekday>(weekdays);
        }

        /// <summary>
        /// Create a ViewModel from a schedule.
        /// </summary>
        /// <param name="schedule"></param>
        /// <returns></returns>
        public static ScheduleViewModel CreateFrom(Schedule schedule)
        {
            var vm = new ScheduleViewModel();
            vm.UpdateFrom(schedule);
            return vm;
        }

        /// <summary>
        /// Compute a MultiCourseViewModel for the dates that happen at the specified time.
        /// </summary>
        /// <param name="schedule"></param>
        /// <param name="selectedCoursesByCourse">A dictionary that maps a course to the SelectedCourse object associated with the user.</param>
        /// <param name="dayOfWeek">Day of the week to look at</param>
        /// <param name="halfHour">The number of half hours since EarliestTime, only respecting the hour and minute of that property.</param>
        /// <returns></returns>
        private static MultiCourseViewModel GetCoursesAt(Schedule schedule, IDictionary<Course, SelectedCourse> selectedCoursesByCourse, DayOfWeek dayOfWeek, int halfHour)
        {
            var timeToFind = schedule.EarliestTime.AddMinutes(30 * halfHour);

            var courses = schedule.DatesByDay[dayOfWeek];
            var courseList = courses.ToList();
            var startingDate = courseList.Find(date => date.From.Hour == timeToFind.Hour && date.From.Minute == timeToFind.Minute);
            if (startingDate == null)
            {
                return null;
            }

            // We've found a matching course, now find overlapping courses
            var datesInFoundDateInterval = new List<CourseViewModel> { ConvertToViewModelCourse(startingDate, selectedCoursesByCourse[startingDate.Course]) };
            // Ensure we're not adding our originally found course as an overlapping course
            courseList.Remove(startingDate);
            for (var i = 0; i < startingDate.LengthInHalfHours(); i++)
            {
                var overlappingTimeToFind = timeToFind.AddMinutes(i * 30);
                var overlappingDate =
                    courseList.Find(
                        course => course.From.Hour == overlappingTimeToFind.Hour && course.From.Minute == overlappingTimeToFind.Minute);
                if (overlappingDate != null)
                {
                    datesInFoundDateInterval.Add(ConvertToViewModelCourse(overlappingDate, selectedCoursesByCourse[overlappingDate.Course]));
                }
            }

            return new MultiCourseViewModel(datesInFoundDateInterval);
        }

        /// <summary>
        /// Generate a CourseViewModel from the Date with the associated SelectedCourse.
        /// </summary>
        /// <param name="date"></param>
        /// <param name="course"></param>
        /// <returns></returns>
        private static CourseViewModel ConvertToViewModelCourse(Date date, SelectedCourse course)
        {
            if (date.Course.Id != course.Course.Id)
            {
                throw new ArgumentException("SelectedCourse doesn't match course in the date");
            }
            return new CourseViewModel(date.Course.Id, date.Course.Name, date.From, date.To, course.Users.Select(x => x.User.Name).ToList());
        }

        /// <summary>
        /// Class describing the day of a user.
        /// </summary>
        public class Weekday : BindableBase
        {
            public DayOfWeek DayOfWeek { get; }

            /// <summary>
            /// Name of the day to display
            /// </summary>
            public string Description { get; }

            /// <summary>
            /// List of MultiCourse objects in this schedule. Empty half hours are 
            /// represented by empty MultiCourses, while non-empty MultiCourses may span 
            /// multiple half hours.
            /// </summary>
            public ObservableCollection<MultiCourseViewModel> MultiCourses { get; }

            public Weekday(DayOfWeek dayOfWeek, string description, IEnumerable<MultiCourseViewModel> multiCourses)
            {
                DayOfWeek = dayOfWeek;
                Description = description;
                MultiCourses = new ObservableCollection<MultiCourseViewModel>(multiCourses);
            }
        }

        public class MultiCourseViewModel
        {
            /// <summary>
            /// A list of courses contained in this MultiCourse.
            /// </summary>
            public List<CourseViewModel> Courses { get; }

            /// <summary>
            /// Earliest Begin of all courses contained in this object
            /// </summary>
            public DateTime? Begin => Courses?.Select(c => c.Begin).Min();

            /// <summary>
            /// Latest End of all courses contained in this object
            /// </summary>
            public DateTime? End => Courses?.Select(c => c.End).Max();

            /// <summary>
            /// End - Begin divided by 30 minutes (integer division).
            /// </summary>
            public int? LengthInHalfHours => End != null && Begin != null ? ((int)(End.Value.AtDate(Begin.Value.Day, Begin.Value.Month, Begin.Value.Year) - Begin).Value.TotalMinutes) / 30 : (int?)null;

            /// <summary>
            /// True iff no courses are contained within this object.
            /// </summary>
            public bool Empty => Courses == null || !Courses.Any();

            public MultiCourseViewModel(IEnumerable<CourseViewModel> courses = null)
            {
                if (courses != null)
                {
                    var courseViewModels = courses as IList<CourseViewModel> ?? courses.ToList();
                    Courses = courseViewModels?.ToList();
                    foreach (var viewModelCourse in courseViewModels)
                    {
                        viewModelCourse.HalfHourOffset = ((int)(viewModelCourse.Begin - Begin).Value.TotalMinutes) / 30;
                    }
                }
            }
        }

        public class CourseViewModel
        {
            /// <summary>
            /// Title to be shown to the user.
            /// </summary>
            public string Title { get; }

            /// <summary>
            /// Time to be shown to the user. Usually something like "11:00 - 13:00).
            /// </summary>
            public string Time { get; }

            public DateTime Begin { get; }

            public DateTime End { get; }

            /// <summary>
            /// List of users participating this course in the schedule
            /// </summary>
            public string Users { get; }

            /// <summary>
            /// Offset in numer of half hours of the Begin of this course relative to the MultiCourse
            /// containing it.
            /// </summary>
            public int HalfHourOffset { get; set; }

            public int LengthInHalfHours => ((int)(End - Begin).TotalMinutes) / 30;

            /// <summary>
            /// ID of this course in the database.
            /// </summary>
            public string Id { get; }

            public CourseViewModel(string id, string title, DateTime begin, DateTime end, IEnumerable<string> users)
            {
                Title = title;
                Begin = begin;
                End = end;
                Users = users.JoinToString(separator: ", ");
                Time = $"{begin.ToString("t")} - {end.ToString("t")}";
                Id = id;
            }
        }
    }
}
