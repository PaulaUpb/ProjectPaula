using System;
using System.Collections.Generic;
using System.Linq;
using ProjectPaula.Model;
using System.Collections.ObjectModel;
using ProjectPaula.Model.ObjectSynchronization;

namespace ProjectPaula.ViewModel
{
    public class ScheduleViewModel : BindableBase
    {
        // The following data structure with the following layout is held in the table:
        //          MON          ...
        // 0:00  Course1, C2
        // 0:30  C1, C2
        // 1:00  C1
        // ...   ...
        private class ScheduleTable
        {
            public int EarliestStartHalfHour { get; }
            public int LatestEndHalfHour { get; }

            public Dictionary<DayOfWeek, IList<ISet<Date>>> DatesByHalfHourByDay { get; }

            public ScheduleTable(int earliestStartHalfHour, int latestEndHalfHour, Dictionary<DayOfWeek, IList<ISet<Date>>> datesByHalfHourByDay)
            {
                EarliestStartHalfHour = earliestStartHalfHour;
                LatestEndHalfHour = latestEndHalfHour;
                DatesByHalfHourByDay = datesByHalfHourByDay;
            }
        }

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

        private static readonly Func<Date, double> DateLengthSelector = date => (date.To.CeilHalfHour() - date.From.FloorHalfHour()).TotalMinutes;

        /// <summary>
        /// EarliestTime, ..., 15:00, 15:30, ..., LatestTime
        /// </summary>
        public ObservableCollectionEx<string> HalfHourTimes { get; } = new ObservableCollectionEx<string>();

        /// <summary>
        /// A collection of Weekdays containing the data about courses.
        /// </summary>
        public ObservableCollectionEx<Weekday> Weekdays { get; } = new ObservableCollectionEx<Weekday>();


        /// <summary>
        /// This method computes the ScheduleTable for the given Schedule.
        /// <see cref="ScheduleTable"/>
        /// </summary>
        /// <param name="schedule"></param>
        /// <returns></returns>
        private static ScheduleTable ComputeDatesByHalfHourByDay(Schedule schedule)
        {
            // Init data structures
            var earliestStartHalfHour = 18;
            var latestEndHalfHour = 36;

            var datesByHalfHourByDay = new Dictionary<DayOfWeek, IList<ISet<Date>>>();
            foreach (var dayOfWeek in DaysOfWeek)
            {
                datesByHalfHourByDay[dayOfWeek] = new List<ISet<Date>>(48);
                for (var i = 0; i < 48; i++)
                {
                    datesByHalfHourByDay[dayOfWeek].Add(new HashSet<Date>());
                }
            }

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

            return new ScheduleTable(earliestStartHalfHour, latestEndHalfHour, datesByHalfHourByDay);
        }


        /// <summary>
        /// Update this ViewModel to match the data in the schedule.
        /// </summary>
        /// <param name="schedule"></param>
        public void UpdateFrom(Schedule schedule)
        {

            var selectedCoursesByCourses = schedule.SelectedCourses.ToDictionary(selectedCourse => selectedCourse.Course);
            var scheduleTable = ComputeDatesByHalfHourByDay(schedule);
            var earliestStartHalfHour = scheduleTable.EarliestStartHalfHour;
            var latestEndHalfHour = scheduleTable.LatestEndHalfHour;
            var datesByHalfHourByDay = scheduleTable.DatesByHalfHourByDay;

            // Recompute HalfHourTimes
            HalfHourTimes.Clear();
            var today = new DateTime();
            var hour = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0).AddMinutes(earliestStartHalfHour * 30);

            HalfHourTimes.AddRange(Enumerable
                .Range(earliestStartHalfHour, latestEndHalfHour - 1)
                .Select(i => (hour + TimeSpan.FromMinutes(i * 30)).ToString("t")));

            // TODO: Remove
            //for (var i = earliestStartHalfHour; i < latestEndHalfHour; i++)
            //{
            //    HalfHourTimes.Add(hour.ToString("t"));
            //    hour = hour.AddMinutes(30);
            //}

            // Recreate course view models
            var columnsForDates = ComputeColumnsForDates(datesByHalfHourByDay);

            Weekdays.Clear();
            var weekdays = new List<Weekday>();

            foreach (var dayOfWeek in DaysOfWeek)
            {
                var isDayEmpty = true;

                // Init 
                var courseViewModelsByHour = new List<ISet<CourseViewModel>>();
                for (var halfHour = 0; halfHour < 48; halfHour++)
                {
                    courseViewModelsByHour.Add(new HashSet<CourseViewModel>());
                }


                var datesByHalfHour = datesByHalfHourByDay[dayOfWeek];
                var takenSpacePercent = new List<int>(Enumerable.Repeat(element: 0, count: 48));

                foreach (var date in datesByHalfHour
                 .SelectMany(dates => dates)
                 .Distinct()
                 .OrderByDescending(DateLengthSelector))
                {
                    isDayEmpty = false;
                    var flooredFrom = date.From.FloorHalfHour();
                    var halfHourComputed = (flooredFrom.Hour * 60 + flooredFrom.Minute) / 30;

                    var lengthInHalfHours = (int)(date.To.CeilHalfHour() - date.From.FloorHalfHour()).TotalMinutes / 30;

                    var maxOverlappingDates = 0;
                    for (var halfHour2 = halfHourComputed; halfHour2 < halfHourComputed + lengthInHalfHours; halfHour2++)
                    {
                        maxOverlappingDates = Math.Max(maxOverlappingDates, datesByHalfHour[halfHour2].Count - 1);
                    }

                    // Doesn't currently yield correct results, falling back to unified column size
                    var offsetPercentX = Enumerable.Range(halfHourComputed, lengthInHalfHours).Select(halfHour2 => takenSpacePercent[halfHour2]).Max();
                    for (var halfHour2 = halfHourComputed; halfHour2 < halfHourComputed + lengthInHalfHours; halfHour2++)
                    {
                        takenSpacePercent[halfHour2] += 100 / (maxOverlappingDates + 1);
                    }


                    var course = date.Course;
                    var overlappingDates = maxOverlappingDates;
                    var offsetHalfHourY = halfHourComputed - earliestStartHalfHour;
                    var users = selectedCoursesByCourses[course].Users.Select(user => user.User.Name);

                    var courseViewModel = new CourseViewModel(course.Id, course.Name, date.From, date.To, users, lengthInHalfHours, overlappingDates, offsetHalfHourY, columnsForDates[date], offsetPercentX);
                    courseViewModelsByHour[halfHourComputed].Add(courseViewModel);
                }

                weekdays.Add(new Weekday(dayOfWeek, courseViewModelsByHour, isDayEmpty));

            }

            if (weekdays[6].IsDayEmpty)
            {
                weekdays.RemoveAt(6);

                if (weekdays[5].IsDayEmpty)
                {
                    weekdays.RemoveAt(5);
                }
            }

            Weekdays.AddRange(weekdays);
        }

        /// <summary>
        /// This method computes the appropriate column for each date in the schedule.
        /// </summary>
        /// <param name="datesByHalfHourByDay"></param>
        /// <returns></returns>
        private static Dictionary<Date, int> ComputeColumnsForDates(IReadOnlyDictionary<DayOfWeek, IList<ISet<Date>>> datesByHalfHourByDay)
        {
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
                     .OrderByDescending(DateLengthSelector)
                     .ToList();
            var columnsForDates = new Dictionary<Date, int>(sortedDates.Count);
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
                        columnsForDates[date] = column;
                        break;
                    }
                }
            }
            return columnsForDates;
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

            public bool IsDayEmpty { get; }

            public Weekday(DayOfWeek dayOfWeek, IList<ISet<CourseViewModel>> courseViewModelsByHour, bool isDayEmpty)
            {
                DayOfWeek = dayOfWeek;
                Description = dayOfWeek.ToString("G");
                CourseViewModelsByHour = courseViewModelsByHour;
                IsDayEmpty = isDayEmpty;

                var allColumnCounts = courseViewModelsByHour
                    .SelectMany(viewModels => viewModels)
                    .Select(viewModel => viewModel.Column)
                    .ToList();
                ColumnCount = allColumnCounts.Any() ? allColumnCounts.Max() + 1 : 1;
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

            public int OverlappingDatesCount { get; }

            public int OffsetHalfHourY { get; }

            public int OffsetPercentX { get; }

            public int Column { get; set; }

            /// <summary>
            /// ID of this course in the database.
            /// </summary>
            public string Id { get; }

            public CourseViewModel(string id, string title, DateTime begin, DateTime end, IEnumerable<string> users, int lengthInHalfHours, int overlappingDatesCount, int offsetHalfHourY, int column, int offsetPercentX)
            {
                Title = title;
                Begin = begin;
                End = end;
                LengthInHalfHours = lengthInHalfHours;
                OverlappingDatesCount = overlappingDatesCount;
                OffsetHalfHourY = offsetHalfHourY;
                Column = column;
                OffsetPercentX = offsetPercentX;
                Users = string.Join(", ", users);
                Time = $"{begin.ToString("t")} - {end.ToString("t")}";
                Id = id;
            }
        }
    }
}
