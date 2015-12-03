using System;
using System.Collections.Generic;
using System.Linq;
using ProjectPaula.Model;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using ProjectPaula.Model.ObjectSynchronization;

namespace ProjectPaula.ViewModel
{
    public class ScheduleViewModel : BindableBase
    {
        
        private const int PaddingHalfHours = 4;

        /// <summary>
        /// Enumeration of all days of the week in the order they appear
        /// in the calender, starting with Monday.
        /// </summary>
        private static readonly List<DayOfWeek> DaysOfWeek = new List<DayOfWeek>()
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
            DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
        };

        /// <summary>
        /// EarliestTime, ..., 15:00, 15:30, ..., LatestTime
        /// </summary>
        public ObservableCollection<string> HalfHourTimes { get; } = new ObservableCollection<string>();

        /// <summary>
        /// A collection of Weekdays containing the data about courses.
        /// </summary>
        public ObservableCollection<Weekday> Weekdays { get; } = new ObservableCollection<Weekday>();

        /// <summary>
        /// Update this ViewModel to match the data in the schedule.
        /// </summary>
        /// <param name="schedule"></param>
        public void UpdateFrom(Schedule schedule)
        {
            // Init data structures
            // The following data structure has the following layout:
            //          MON          ...
            // 0:00  Course1, C2
            // 0:30  C1, C2
            // 1:00  C1
            // ...   ...
            var datesByHalfHourByDay = new Dictionary<DayOfWeek, IList<ISet<Date>>>();
            foreach (var dayOfWeek in DaysOfWeek)
            {
                datesByHalfHourByDay[dayOfWeek] = new List<ISet<Date>>(48);
                for (var i = 0; i < 48; i++)
                {
                    datesByHalfHourByDay[dayOfWeek].Add(new HashSet<Date>());
                }
            }
            var selectedCoursesByCourses = schedule.SelectedCourses.ToDictionary(selectedCourse => selectedCourse.Course);

            // Compute table
            var earliestStartHalfHour = 18;
            var latestEndHalfHour = 36;
            foreach (var courseDate in schedule.SelectedCourses.SelectMany(selectedCourse => selectedCourse.Course.RegularDates).Select(x => x.Key))
            {
                var flooredFrom = courseDate.From.FloorHalfHour();
                var ceiledTo = courseDate.To.CeilHalfHour();
                var dayOfDate = flooredFrom.DayOfWeek;
                var firstHourOfDate = (flooredFrom.Hour * 60 + flooredFrom.Minute) / 30;
                var lastHourOfDate = (ceiledTo.Hour * 60 + ceiledTo.Minute) / 30;

                earliestStartHalfHour = Math.Min(earliestStartHalfHour, firstHourOfDate - PaddingHalfHours);
                latestEndHalfHour = Math.Max(latestEndHalfHour, lastHourOfDate + PaddingHalfHours);

                for (var halfHour = firstHourOfDate; halfHour < lastHourOfDate; halfHour++)
                {
                    datesByHalfHourByDay[dayOfDate][halfHour].Add(courseDate);
                }
            }

            HalfHourTimes.Clear();
            var today = new DateTime();
            var hour = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0).AddMinutes(earliestStartHalfHour * 30);
            for (var i = earliestStartHalfHour; i < latestEndHalfHour; i++)
            {
                HalfHourTimes.Add(hour.ToString("t"));
                hour = hour.AddMinutes(30);
            }

            // Recreate course view models

            // For each day of the week, this contains a list of columns with a list of rows and the value
            // true, if and only if the cell is already used by a date
            var blockedCellsByDay = new Dictionary<DayOfWeek, List<List<bool>>>();
            foreach (var dayOfWeek in DaysOfWeek)
            {
                blockedCellsByDay[dayOfWeek] = new List<List<bool>>();
            }

            var sortedDates =
                DaysOfWeek.Select(day => datesByHalfHourByDay[day])
                    .SelectMany(datesByHalfHour => datesByHalfHour)
                    .SelectMany(dates => dates)
                    .Distinct()
                    .OrderByDescending(date => (date.To.CeilHalfHour() - date.From.FloorHalfHour()).TotalMinutes)
                    .ToList();
            var columnForDate = new Dictionary<Date, int>(sortedDates.Count);
            foreach (var date in sortedDates)
            {
                // Reserve a column
                var flooredFrom = date.From.FloorHalfHour();
                var dayOfWeek = date.From.DayOfWeek;
                var halfHour = (flooredFrom.Hour * 60 + flooredFrom.Minute) / 30;

                var lengthInHalfHours = (int)(date.To.CeilHalfHour() - flooredFrom).TotalMinutes / 30;
                for (var column = 0; column < blockedCellsByDay[dayOfWeek].Count + 1; column++)
                {
                    if (column == blockedCellsByDay[dayOfWeek].Count)
                    {
                        blockedCellsByDay[dayOfWeek].Add(Enumerable.Repeat(false, 48).ToList());
                    }

                    var columnCopy = blockedCellsByDay[dayOfWeek][column].ToList();
                    var reserveError = false;
                    for (var halfHour2 = halfHour; !reserveError && halfHour2 < halfHour + lengthInHalfHours; halfHour2++)
                    {
                        if (!columnCopy[halfHour2])
                        {
                            // Reserve half hour
                            columnCopy[halfHour2] = true;
                        }
                        else
                        {
                            // Error: Half hour was already reserved
                            reserveError = true;
                        }
                    }

                    if (!reserveError)
                    {
                        // Reservation was successful, apply changes
                        blockedCellsByDay[dayOfWeek][column] = columnCopy;
                        columnForDate[date] = column;
                        break;
                    }

                }

            }

            Weekdays.Clear();
            foreach (var dayOfWeek in DaysOfWeek)
            {
                // Init 
                var courseViewModelsByHour = new List<ISet<CourseViewModel>>();
                for (var halfHour = 0; halfHour < 48; halfHour++)
                {
                    courseViewModelsByHour.Add(new HashSet<CourseViewModel>());
                }


                var datesByHour = datesByHalfHourByDay[dayOfWeek];
                var visitedDates = new List<Date>();
                for (var halfHour = 0; halfHour < datesByHour.Count; halfHour++)
                {
                    var dates = datesByHour[halfHour];
                    foreach (var date in dates)
                    {
                        if (visitedDates.Contains(date))
                        {
                            continue;
                        }

                        // Reserve a column
                        var lengthInHalfHours = (int)(date.To.CeilHalfHour() - date.From.FloorHalfHour()).TotalMinutes / 30;



                        var maxOverlappingDates = 0;
                        for (var halfHour2 = halfHour; halfHour2 < halfHour + lengthInHalfHours; halfHour2++)
                        {
                            maxOverlappingDates = Math.Max(maxOverlappingDates, datesByHour[halfHour2].Count - 1);
                        }


                        var course = date.Course;
                        var widthPercent = maxOverlappingDates;
                        var offsetHalfHourY = halfHour - earliestStartHalfHour;
                        var users = selectedCoursesByCourses[course].Users.Select(user => user.User.Name);

                        var courseViewModel = new CourseViewModel(course.Id, course.Name, date.From, date.To, users, lengthInHalfHours, widthPercent, offsetHalfHourY, columnForDate[date]);
                        courseViewModelsByHour[halfHour].Add(courseViewModel);
                        visitedDates.Add(date);
                    }
                }

                Weekdays.Add(new Weekday(dayOfWeek, courseViewModelsByHour));
            }
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

        public class Weekday
        {
            public DayOfWeek DayOfWeek { get; }
            public IList<ISet<CourseViewModel>> CourseViewModelsByHour { get; }

            public string Description { get; }

            public int ColumnCount { get; }

            public Weekday(DayOfWeek dayOfWeek, IList<ISet<CourseViewModel>> courseViewModelsByHour)
            {
                DayOfWeek = dayOfWeek;
                Description = dayOfWeek.ToString("G");
                CourseViewModelsByHour = courseViewModelsByHour;

                ColumnCount = courseViewModelsByHour
                        .SelectMany(viewModels => viewModels)
                        .Select(viewModel => viewModel.Column)
                        .Max() + 1;
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

            public int LengthInHalfHours { get; }

            public int OverlappingCoursesCount { get; }

            public int OffsetHalfHourY { get; }

            public int Column { get; set; }

            /// <summary>
            /// ID of this course in the database.
            /// </summary>
            public string Id { get; }

            public CourseViewModel(string id, string title, DateTime begin, DateTime end, IEnumerable<string> users, int lengthInHalfHours, int overlappingCoursesCount, int offsetHalfHourY, int column)
            {
                Title = title;
                Begin = begin;
                End = end;
                LengthInHalfHours = lengthInHalfHours;
                OverlappingCoursesCount = overlappingCoursesCount;
                OffsetHalfHourY = offsetHalfHourY;
                Column = column;
                Users = string.Join(", ", users);
                Time = $"{begin.ToString("t")} - {end.ToString("t")}";
                Id = id;
            }
        }
    }
}
