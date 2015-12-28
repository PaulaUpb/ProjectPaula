using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNet.Server.Kestrel.Http;
using ProjectPaula.Model;
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
        private static readonly List<DayOfWeek> DaysOfWeek = new List<DayOfWeek>
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
        /// A list of course lists, where the inner list describes a list of
        /// tutorials the user still needs to choose from.
        /// </summary>
        private readonly List<List<Course>> _pendingTutorials = new List<List<Course>>();

        /// <summary>
        /// Add a list of tutorials to be displayed as pending options
        /// the user can choose from.
        /// </summary>
        /// <param name="pendingTutorials"></param>
        public void AddPendingTutorials(List<Course> pendingTutorials)
        {
            _pendingTutorials.Add(pendingTutorials);
        }

        /// <summary>
        /// Remove the first pending tutorial collection containg this
        /// tutorial.
        /// </summary>
        /// <param name="pendingTutorial"></param>
        public void RemovePendingTutorials(Course pendingTutorial)
        {
            var courses = _pendingTutorials.FirstOrDefault(it => it.Contains(pendingTutorial));
            if (courses != null)
            {
                _pendingTutorials.Remove(courses);
            }
        }


        /// <summary>
        /// This method computes the ScheduleTable for the given Schedule.
        /// <see cref="ScheduleTable"/>
        /// </summary>
        /// <param name="schedule"></param>
        /// <returns></returns>
        private ScheduleTable ComputeDatesByHalfHourByDay(Schedule schedule)
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

            foreach (var courseDate in schedule.SelectedCourses.SelectMany(selectedCourse => selectedCourse.Course.RegularDates).Select(x => x.Key)
                                        .Concat(_pendingTutorials.SelectMany(it => it).SelectMany(it => it.RegularDates).Select(x => x.Key)))
            {
                var flooredFrom = courseDate.From.FloorHalfHour();
                var ceiledTo = courseDate.To.CeilHalfHour();
                var dayOfDate = flooredFrom.DayOfWeek;
                var firstHourOfDate = (flooredFrom.Hour * 60 + flooredFrom.Minute) / 30;
                var lastHourOfDate = (ceiledTo.Hour * 60 + ceiledTo.Minute) / 30;

                earliestStartHalfHour = Math.Max(0, Math.Min(earliestStartHalfHour, firstHourOfDate - PaddingHalfHours));
                latestEndHalfHour = Math.Min(48, Math.Max(latestEndHalfHour, lastHourOfDate + PaddingHalfHours));

                for (var halfHour = firstHourOfDate; halfHour < lastHourOfDate; halfHour++)
                {
                    datesByHalfHourByDay[dayOfDate][halfHour].Add(courseDate);
                }
            }

            return new ScheduleTable(earliestStartHalfHour, latestEndHalfHour, datesByHalfHourByDay);
        }

        /// <summary>
        /// Check if the specified course has any actual overlaps with
        /// dates of non-pending courses.
        /// </summary>
        private static bool HasOverlapsWithNonPending(Dictionary<Date, ISet<Date>> overlappingDates, Date date, ISet<Course> pendingCourses)
        {
            return overlappingDates.Any(
                overlappingDateGroup => Equals(overlappingDateGroup.Key, date) && overlappingDateGroup.Value.Any(date2 => !pendingCourses.Contains(date2.Course))
                || !pendingCourses.Contains(overlappingDateGroup.Key.Course) && overlappingDateGroup.Value.Any(date2 => date2.Equals(date))
                );
        }

        /// <summary>
        /// Find all actually overlapping dates, not simply the ones
        /// appearing in the same half hour slot. The key is a representant
        /// for all the dates it collides with, not including itself.
        /// </summary>
        /// <param name="scheduleTable">The precomputed ScheduleTable</param>
        private static Dictionary<Date, ISet<Date>> FindOverlappingDates(ScheduleTable scheduleTable)
        {
            var result = new Dictionary<Date, ISet<Date>>();

            var halfHourDatas = DaysOfWeek.SelectMany(day => scheduleTable.DatesByHalfHourByDay[day])
                .Where(hourData => hourData.Count > 1)
                .Select(hourData => hourData.ToList())
                .ToList();
            foreach (var halfHourData in halfHourDatas)
            {
                // hourData contains courses which may overlap
                // so iterate over each pair of them and count the number of overlapping
                // dates
                for (var i = 0; i < halfHourData.Count; i++)
                {
                    var course1 = halfHourData[i].Course;
                    var course1DatesAtHalfHour = course1.RegularDates.Find(group => group.Key.Equals(halfHourData[i])).ToList();
                    for (var j = i + 1; j < halfHourData.Count; j++)
                    {
                        var course2 = halfHourData[j].Course;
                        var course2DatesAtHalfHour = course2.RegularDates.Find(group => group.Key.Equals(halfHourData[j])).ToList();

                        // We now go a list of all dates course1 and course2
                        // have at the potentially colliding half hour slot,
                        // so we now iterate over the pairs of dates in the semester
                        // to find actually colliding ones as they could be 
                        // in alternating weeks
                        foreach (var course1Date in course1DatesAtHalfHour)
                        {
                            foreach (var course2Date in course2DatesAtHalfHour
                                .Where(course2Date => course1Date.From.DayOfYear == course2Date.From.DayOfYear
                                        && course1Date.From.Year == course2Date.From.Year))
                            {
                                // Overlap detected
                                if (!result.ContainsKey(course1Date))
                                {
                                    result[course1Date] = new HashSet<Date>();
                                }
                                result[course1Date].Add(course2Date);
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Update this ViewModel to match the data in the schedule.
        /// </summary>
        /// <param name="schedule"></param>
        public void UpdateFrom(Schedule schedule)
        {
            var selectedCoursesByCourses = schedule.SelectedCourses.ToDictionary(selectedCourse => selectedCourse.Course);
            var allPendingTutorials = _pendingTutorials.SelectMany(it => it).ToImmutableHashSet();
            var scheduleTable = ComputeDatesByHalfHourByDay(schedule);
            var earliestStartHalfHour = scheduleTable.EarliestStartHalfHour;
            var latestEndHalfHour = scheduleTable.LatestEndHalfHour;
            var datesByHalfHourByDay = scheduleTable.DatesByHalfHourByDay;
            var actuallyOverlappingDates = FindOverlappingDates(scheduleTable);

            // Recompute HalfHourTimes
            HalfHourTimes.Clear();
            var today = new DateTime();
            var hour = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0);

            HalfHourTimes.AddRange(Enumerable
                .Range(earliestStartHalfHour, latestEndHalfHour - earliestStartHalfHour - 1)
                .Select(i => (hour + TimeSpan.FromMinutes(i * 30)).ToString("t")));

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
                var takenSpacePercent = new List<int>(Enumerable.Repeat(0, 48));

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
                    var tutorials = course.FindAllTutorials().ToList();
                    var overlappingDates = maxOverlappingDates;
                    var offsetHalfHourY = halfHourComputed - earliestStartHalfHour;
                    var users = selectedCoursesByCourses.ContainsKey(course) ?
                        selectedCoursesByCourses[course].Users.Select(user => user.User.Name) :
                        Enumerable.Empty<string>();
                    var datesInInterval = course.RegularDates.First(x => Equals(x.Key, date)).ToList();
                    var isPending = allPendingTutorials.Contains(course);
                    var hasOverlaps = HasOverlapsWithNonPending(actuallyOverlappingDates, date, allPendingTutorials);
                    var discourageSelection = course.IsTutorial && isPending && hasOverlaps;
                    var showDisplayTutorials = !course.IsTutorial && tutorials.Count > 0 && !tutorials.Any(tutorial =>
                                 allPendingTutorials.Contains(tutorial) || selectedCoursesByCourses.ContainsKey(tutorial));

                    var courseViewModel = new CourseViewModel(course.Id, course.Name, date.From, date.To,
                        users, lengthInHalfHours, overlappingDates, offsetHalfHourY, columnsForDates[date],
                        offsetPercentX, datesInInterval, isPending, discourageSelection, hasOverlaps, course.IsTutorial,
                        showDisplayTutorials);
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
            /// Time to be shown to the user. Usually something like "11:00 - 13:00, weekly".
            /// </summary>
            public string Time { get; }

            public DateTimeOffset Begin { get; }

            public DateTimeOffset End { get; }

            /// <summary>
            /// List of users participating this course in the schedule
            /// </summary>
            public string Users { get; }

            public int LengthInHalfHours { get; }

            public int OverlappingDatesCount { get; }

            public int OffsetHalfHourY { get; }

            public int OffsetPercentX { get; }

            public int Column { get; set; }

            public bool IsPending { get; }

            /// <summary>
            /// True iff the course is a tutorial and collides with another already selected course
            /// </summary>
            public bool DiscourageSelection { get; }

            public bool HasOverlaps { get; }

            public List<string> AllDates { get; }

            public bool IsTutorial { get; }

            public bool ShowDisplayTutorials { get; }

            /// <summary>
            /// ID of this course in the database.
            /// </summary>
            public string Id { get; }

            public CourseViewModel(string id, string title, DateTimeOffset begin, DateTimeOffset end, IEnumerable<string> users, int lengthInHalfHours, int overlappingDatesCount, int offsetHalfHourY, int column, int offsetPercentX, IList<Date> dates, bool isPending, bool discourageSelection, bool hasOverlaps, bool isTutorial, bool showDisplayTutorials)
            {
                Title = title;
                Begin = begin;
                End = end;
                LengthInHalfHours = lengthInHalfHours;
                OverlappingDatesCount = overlappingDatesCount;
                OffsetHalfHourY = offsetHalfHourY;
                Column = column;
                OffsetPercentX = offsetPercentX;
                IsPending = isPending;
                DiscourageSelection = discourageSelection;
                HasOverlaps = hasOverlaps;
                IsTutorial = isTutorial;
                ShowDisplayTutorials = showDisplayTutorials;
                Users = string.Join(", ", users);
                Time = $"{begin.ToString("t")} - {end.ToString("t")}, {ComputeIntervalDescription(dates)}";
                AllDates = dates.OrderBy(date => date.From).Select(date => date.From.ToString("dd.MM.yy")).ToList();
                Id = id;
            }

            /// <summary>
            /// Compute a description for the intervals this course date happens.
            /// </summary>
            /// <param name="dates"></param> Dates that are on the same day of the week
            /// <returns>Something like "[2-]wöchentlich[, mit Ausnahmen]" or "unregelmäßig"</returns>
            public static string ComputeIntervalDescription(IList<Date> dates)
            {
                if (dates.Count == 1)
                {
                    return $"nur am {dates[0].From.ToString("dd.MM.yy")}";
                }
                var orderedDates = dates.OrderBy(date => date.From).ToList();


                var ruleExceptions = 0;
                var interval = (orderedDates[1].From - orderedDates[0].From).Days;
                var triedIntervals = new List<int> { interval };
                for (var i = 1; i < orderedDates.Count - 1; i++)
                {
                    if (orderedDates[i + 1].From.DayOfWeek != orderedDates[i].From.DayOfWeek)
                    {
                        throw new ArgumentException("All dates must be on the same day of the week.", nameof(dates));
                    }

                    var thisInterval = (orderedDates[i + 1].From - orderedDates[i].From).Days;
                    if (thisInterval != interval)
                    {
                        ruleExceptions++;

                        if (ruleExceptions / (double)orderedDates.Count > 0.5 && !triedIntervals.Contains(thisInterval))
                        {
                            // Too many exceptions to the rule, try another interval
                            interval = thisInterval;
                            ruleExceptions = 0;
                            i = 1;
                            triedIntervals.Add(interval);
                        }
                    }
                }

                var exceptionRate = ruleExceptions / (double)orderedDates.Count;
                var intervalDescription = interval / 7 == 1 ? "wöchentlich" : $"{interval}-wöchentlich";

                if (exceptionRate > 0.15)
                {
                    return "unregelmäßig";
                }
                if (ruleExceptions > 2) // Deal with vacations
                {
                    return $"{intervalDescription}, mit Ausnahmen";
                }
                return intervalDescription;
            }
        }
    }
}
