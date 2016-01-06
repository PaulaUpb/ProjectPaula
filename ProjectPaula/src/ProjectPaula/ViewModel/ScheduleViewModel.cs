using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNet.Server.Kestrel.Http;
using ProjectPaula.Model;
using ProjectPaula.Model.ObjectSynchronization;
using System.Globalization;

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

            /// <summary>
            /// Compute the days of the week that have been changed between
            /// the the two tables.
            /// </summary>
            /// <param name="scheduleTable">Schedule Table to diff</param>
            /// <param name="pendingChangesDifference">OldPendingChanges\NewPendingChanges + NewPendingChanges\OldPendingChanges</param>
            /// <returns></returns>
            public IEnumerable<DayOfWeek> ChangedDays(ScheduleTable scheduleTable, IEnumerable<Course> pendingChangesDifference)
            {
                var allChangedPending = pendingChangesDifference.ToImmutableHashSet();
                return (from dayOfWeek in DaysOfWeek
                        let ownDatesByHalfHour = DatesByHalfHourByDay[dayOfWeek]
                        let strangerDatesByHalfHour = scheduleTable.DatesByHalfHourByDay[dayOfWeek]
                        where ownDatesByHalfHour.Count != strangerDatesByHalfHour.Count ||
                              ownDatesByHalfHour.Where(
                                  (t, i) => t.Count != strangerDatesByHalfHour[i].Count ||
                                            t.SymmetricDifference(strangerDatesByHalfHour[i]).Any() ||
                                            t.Select(it => it.Course).Intersect(allChangedPending).Any() ||
                                            strangerDatesByHalfHour[i].Select(it => it.Course).Intersect(allChangedPending).Any()
                                  ).Any()
                        select dayOfWeek);
            }
        }

        private const int PaddingHalfHours = 2;
        private const int DefaultEarliestHalfHour = 24;
        private const int DefaultLatestHalfHour = 36;

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

        private int _earliestHalfhour = DefaultEarliestHalfHour;

        /// <summary>
        /// The earliest half hour a course in this schedule has.
        /// </summary>
        public int EarliestHalfHour
        {
            get { return _earliestHalfhour; }
            set { Set(ref _earliestHalfhour, value); }
        }

        private int _latestHalfHour = DefaultLatestHalfHour;

        /// <summary>
        /// The latest half hour a course in this schedule has.
        /// </summary>
        public int LatestHalfHour
        {
            get { return _latestHalfHour; }
            set { Set(ref _latestHalfHour, value); }
        }

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
        /// List of pending tutorials that have been removed or changed plus all courses that have had their
        /// users changed since the last call to UpdateFrom.
        /// </summary>
        private readonly List<Course> _changedPendingTutorialsAndCourseUsers = new List<Course>();

        private Dictionary<Course, List<int>> _usersByCourses = new Dictionary<Course, List<int>>();

        private ScheduleTable _scheduleTable;

        /// <summary>
        /// Add a list of tutorials to be displayed as pending options
        /// the user can choose from. The caller needs to update this viewmodel using
        /// UpdateFrom(Schedule) afterwards.
        /// </summary>
        /// <param name="pendingTutorials"></param>
        public void AddPendingTutorials(List<Course> pendingTutorials)
        {
            _pendingTutorials.Add(pendingTutorials);
            lock (_changedPendingTutorialsAndCourseUsers)
            {
                _changedPendingTutorialsAndCourseUsers.AddRange(pendingTutorials);
            }
        }

        /// <summary>
        /// Remove the first pending tutorial collection containg this
        /// tutorial. The caller needs to update this viewmodel using
        /// UpdateFrom(Schedule) afterwards.
        /// </summary>
        /// <param name="pendingTutorial"></param>
        public void RemovePendingTutorials(Course pendingTutorial)
        {
            var courses = _pendingTutorials.FirstOrDefault(it => it.Contains(pendingTutorial));
            if (courses != null)
            {
                _pendingTutorials.Remove(courses);
                lock (_changedPendingTutorialsAndCourseUsers)
                {
                    _changedPendingTutorialsAndCourseUsers.AddRange(courses);
                }
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
            var earliestStartHalfHour = DefaultEarliestHalfHour;
            var latestEndHalfHour = DefaultLatestHalfHour;

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
        /// Check if the specified course date has any actual overlaps with
        /// dates of non-pending courses.
        /// </summary>
        /// <param name="date">Representant for a group of dates on the same day, at the same time</param>
        private static int OverlapsWithNonPending(ICollection<ISet<Date>> overlappingDates, Date date, ICollection<Course> pendingCourses)
        {
            return overlappingDates.Count(overlappingDateGroup => overlappingDateGroup.Any(it => Date.SameGroup(it, date, sameCourse: true))
                                                                  && overlappingDateGroup.Any(it => !pendingCourses.Contains(it.Course)));
        }

        /// <summary>
        /// Find all actually overlapping dates, not simply the ones
        /// appearing in the same half hour slot. Each item
        /// of the collection is a group of colliding dates.
        /// </summary>
        /// <param name="scheduleTable">The precomputed ScheduleTable</param>
        private static ICollection<ISet<Date>> FindOverlappingDates(ScheduleTable scheduleTable)
        {
            var result = new List<ISet<Date>>();

            foreach (var dayOfWeek in DaysOfWeek)
            {
                for (var halfHour = 0; halfHour < scheduleTable.DatesByHalfHourByDay[dayOfWeek].Count; halfHour++)
                {
                    if (scheduleTable.DatesByHalfHourByDay[dayOfWeek][halfHour].Count < 2)
                    {
                        // Skip half hours without overlaps
                        continue;
                    }
                    var halfHourData = scheduleTable.DatesByHalfHourByDay[dayOfWeek][halfHour].ToList();


                    // hourData contains courses which may overlap
                    // so iterate over each pair of them and count the number of overlapping
                    // dates
                    for (var i = 0; i < halfHourData.Count; i++)
                    {
                        var course1 = halfHourData[i].Course;
                        var course1DatesAtHalfHour =
                            course1.RegularDates.Find(group => group.Key.Equals(halfHourData[i])).ToList();
                        for (var j = i + 1; j < halfHourData.Count; j++)
                        {
                            var course2 = halfHourData[j].Course;
                            var course2DatesAtHalfHour =
                                course2.RegularDates.Find(group => group.Key.Equals(halfHourData[j])).ToList();

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

                                    var overlappingDateGroup = result.FirstOrDefault(group => group.Contains(course1Date) || group.Contains(course2Date));
                                    if (overlappingDateGroup != null)
                                    {
                                        overlappingDateGroup.Add(course1Date);
                                        overlappingDateGroup.Add(course2Date);
                                    }
                                    else
                                    {
                                        result.Add(new HashSet<Date>() { course1Date, course2Date });
                                    }
                                }
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
            var newScheduleTable = ComputeDatesByHalfHourByDay(schedule);
            IEnumerable<DayOfWeek> changedDaysOfWeek;
            lock (_changedPendingTutorialsAndCourseUsers)
            {
                var newUsersByCourses = schedule.SelectedCourses.ToDictionary(it => it.Course, it => it.Users.Select(user => user.User.Id).ToList());
                _changedPendingTutorialsAndCourseUsers.AddRange(
                    newUsersByCourses.Where(
                                         newUsersByCourse =>
                                         {
                                             var symmetricDifference = !_usersByCourses.ContainsKey(newUsersByCourse.Key) ? null : _usersByCourses[newUsersByCourse.Key]
                                                 .SymmetricDifference(newUsersByCourse.Value).ToList();
                                             return !_usersByCourses.ContainsKey(newUsersByCourse.Key) ||
                                                                        symmetricDifference
                                                                            .Any();
                                         })
                                     .Select(newUserByCourse => newUserByCourse.Key)
                    );
                _usersByCourses = newUsersByCourses;

                changedDaysOfWeek = _scheduleTable != null
                    ? newScheduleTable.ChangedDays(_scheduleTable, _changedPendingTutorialsAndCourseUsers)
                    : DaysOfWeek;
                _changedPendingTutorialsAndCourseUsers.Clear();
            }
            _scheduleTable = newScheduleTable;

            EarliestHalfHour = newScheduleTable.EarliestStartHalfHour;
            LatestHalfHour = newScheduleTable.LatestEndHalfHour;
            var datesByHalfHourByDay = newScheduleTable.DatesByHalfHourByDay;
            var actuallyOverlappingDates = FindOverlappingDates(newScheduleTable);

            // Recreate course view models
            var columnsForDates = ComputeColumnsForDates(datesByHalfHourByDay);

            var weekdaysTmp = Weekdays.Count == 0 ? new ObservableCollectionEx<Weekday>(Enumerable.Repeat<Weekday>(null, DaysOfWeek.Count)) : Weekdays;

            foreach (var dayOfWeek in changedDaysOfWeek)
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
                    var users = selectedCoursesByCourses.ContainsKey(course) ?
                        selectedCoursesByCourses[course].Users.Select(user => user.User.Name) :
                        Enumerable.Empty<string>();
                    var datesInInterval = course.RegularDates.First(x => Equals(x.Key, date)).ToList();
                    var isPending = allPendingTutorials.Contains(course);
                    var overlapsWithNonPending = OverlapsWithNonPending(actuallyOverlappingDates, date, allPendingTutorials);
                    var discourageSelection = course.IsTutorial && isPending && overlapsWithNonPending > 0;
                    var showDisplayTutorials = !course.IsTutorial && tutorials.Count > 0 && !tutorials.Any(tutorial =>
                                 allPendingTutorials.Contains(tutorial) || selectedCoursesByCourses.ContainsKey(tutorial));

                    var courseViewModel = new CourseViewModel(course.Id, course.Name, date.From, date.To,
                        users.ToList(), lengthInHalfHours, overlappingDates, halfHourComputed, columnsForDates[date],
                        offsetPercentX, datesInInterval, isPending, discourageSelection, overlapsWithNonPending / (double)datesInInterval.Count, course.IsTutorial,
                        showDisplayTutorials);
                    courseViewModelsByHour[halfHourComputed].Add(courseViewModel);
                }

                var index = dayOfWeek.Position();
                while (index-- > weekdaysTmp.Count)
                {
                    // Day we want to insert is probably a Sunday, but Saturday was removed earlier because it's empty
                    weekdaysTmp.Add(new Weekday(DaysOfWeek[weekdaysTmp.Count], Enumerable.Empty<ISet<CourseViewModel>>().ToList(), true));
                }

                var weekday = new Weekday(dayOfWeek, courseViewModelsByHour, isDayEmpty);
                if (dayOfWeek.Position() == weekdaysTmp.Count)
                {
                    // We inserted everything up to including Saturday, but Sunday is not yet present,
                    // so we need to append it to the list
                    weekdaysTmp.Add(weekday);
                }
                else
                {
                    weekdaysTmp[dayOfWeek.Position()] = weekday;
                }
            }

            if (weekdaysTmp.Count == 7 && weekdaysTmp[6].IsDayEmpty)
            {
                weekdaysTmp.RemoveAt(6);

                if (weekdaysTmp[5].IsDayEmpty)
                {
                    weekdaysTmp.RemoveAt(5);
                }
            }

            if (Weekdays.Count == 0)
            {
                // We're running this method for the first time
                Weekdays.AddRange(weekdaysTmp);
            }
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

            var sortedDates = DaysOfWeek.Select(day => datesByHalfHourByDay[day]).SelectMany(datesByHalfHour => datesByHalfHour).SelectMany(dates => dates).Distinct().OrderByDescending(DateLengthSelector).ToList();
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
            private CultureInfo _cultureInfo = new CultureInfo("de-DE");
            public DayOfWeek DayOfWeek { get; }
            public IList<ISet<CourseViewModel>> CourseViewModelsByHour { get; }

            public string Description { get; }

            public int ColumnCount { get; }

            public bool IsDayEmpty { get; }

            public Weekday(DayOfWeek dayOfWeek, IList<ISet<CourseViewModel>> courseViewModelsByHour, bool isDayEmpty)
            {
                DayOfWeek = dayOfWeek;
                Description = _cultureInfo.DateTimeFormat.GetDayName(dayOfWeek);
                CourseViewModelsByHour = courseViewModelsByHour;
                IsDayEmpty = isDayEmpty;

                var allColumnCounts = courseViewModelsByHour.SelectMany(viewModels => viewModels).Select(viewModel => viewModel.Column).ToList();
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

            public IList<string> UserList { get; }

            public int LengthInHalfHours { get; }

            /// <summary>
            /// The number of overlapping dates, meaning the number of
            /// additional courses in the same row
            /// </summary>
            public int OverlappingDatesCount { get; }

            /// <summary>
            /// Absolute offset to 0:00, measured in half hour steps.
            /// </summary>
            public int OffsetHalfHourY { get; }

            public int OffsetPercentX { get; }

            public int Column { get; set; }

            public bool IsPending { get; }

            /// <summary>
            /// True iff the course is a tutorial and collides with another already selected course
            /// </summary>
            public bool DiscourageSelection { get; }

            /// <summary>
            /// ([Number of overlapping dates with other courses, counting actual collisions, not
            /// just courses in the same row]/[Number of dates this course has on this day])
            /// </summary>
            public double OverlapsQuote { get; }

            public List<string> AllDates { get; }

            public bool IsTutorial { get; }

            public bool ShowDisplayTutorials { get; }

            /// <summary>
            /// ID of this course in the database.
            /// </summary>
            public string Id { get; }

            public CourseViewModel(string id, string title, DateTimeOffset begin, DateTimeOffset end, IList<string> users, int lengthInHalfHours, int overlappingDatesCount, int offsetHalfHourY, int column, int offsetPercentX, IList<Date> dates, bool isPending, bool discourageSelection, double overlapsQuote, bool isTutorial, bool showDisplayTutorials)
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
                OverlapsQuote = overlapsQuote;
                IsTutorial = isTutorial;
                ShowDisplayTutorials = showDisplayTutorials;
                Users = string.Join(", ", users);
                UserList = users;
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
                var intervalDescription = interval / 7 == 1 ? "wöchentlich" : $"{interval}-tägig";

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
