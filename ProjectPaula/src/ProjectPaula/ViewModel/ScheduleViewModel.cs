using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using ProjectPaula.Model;
using ProjectPaula.Model.ObjectSynchronization;
using System.Globalization;
using ProjectPaula.Model.ObjectSynchronization.ChangeTracking;
using Newtonsoft.Json;
using ProjectPaula.Util;

namespace ProjectPaula.ViewModel
{
    public class ScheduleViewModel : BindableBase
    {
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
        /// Date => {Dates overlapping it}
        /// </summary>
        [DoNotTrack, JsonIgnore]
        public Dictionary<Date, ISet<Date>> OverlappingDates { get; private set; }

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
        /// UpdateFrom(Schedule) afterwards. Tutorials already selected by the user
        /// will not be added as pending tutorials.
        /// </summary>
        /// <param name="pendingTutorials"></param>
        /// <param name="evenIfAlreadyInSchedule">If true, even adds tutorials as pending that are already selected.</param>
        public void AddPendingTutorials(List<Course> pendingTutorials, bool evenIfAlreadyInSchedule = false)
        {
            if (!evenIfAlreadyInSchedule && pendingTutorials.Any(tutorial => _scheduleTable.Courses.Contains(tutorial)))
            {
                return;
            }

            _pendingTutorials.Add(pendingTutorials);
            lock (_changedPendingTutorialsAndCourseUsers)
            {
                _changedPendingTutorialsAndCourseUsers.AddRange(pendingTutorials);
            }
        }

        /// <summary>
        /// Remove the first tutorial collection containing this
        /// tutorial from the list of pending tutorial collections.
        /// The caller needs to update this viewmodel using
        /// UpdateFrom(Schedule) afterwards.
        /// </summary>
        /// <param name="pendingTutorial"></param>
        public void RemovePendingTutorials(Course pendingTutorial, ErrorReporter errorReporter)
        {
            if (pendingTutorial == null)
            {
                errorReporter.Throw(
                    new ArgumentNullException(nameof(pendingTutorial)),
                    UserErrorsViewModel.GenericErrorMessage);
            }

            var courses =
                _scheduleTable.Courses.SelectMany(course => course.AllTutorials)
                    .FirstOrDefault(tutorialGroup => tutorialGroup.Contains(pendingTutorial));
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

            var courses = schedule.SelectedCourses.Select(sel => sel.Course).ToList();
            return new ScheduleTable(earliestStartHalfHour, latestEndHalfHour, datesByHalfHourByDay, courses);
        }

        /// <summary>
        /// Check if the specified course date has any actual overlaps with
        /// dates of non-pending courses.
        /// </summary>
        /// <param name="date">Representant for a group of dates on the same day, at the same time</param>
        private static int OverlapsWithNonPending(Dictionary<Date, ISet<Date>> overlappingDates, Date date, ICollection<Course> pendingCourses)
            => overlappingDates.Count(overlappingDateGroup => Date.SameGroup(overlappingDateGroup.Key, date, sameCourse: true)
                                                              && overlappingDateGroup.Value.Any(it => !pendingCourses.Contains(it.Course))
                                      );


        /// <summary>
        /// Find all actually overlapping dates, not simply the ones
        /// appearing in the same half hour slot. Each item
        /// of the collection is a group of colliding dates.
        /// </summary>
        /// <param name="scheduleTable">The precomputed ScheduleTable</param>
        private static Dictionary<Date, ISet<Date>> FindOverlappingDates(ScheduleTable scheduleTable)
        {
            var result = new Dictionary<Date, ISet<Date>>();
            // Key = Day at midnight in ms
            var perHourPerDayTable = new Dictionary<long, IList<ISet<Date>>>();

            foreach (var dayOfWeek in DaysOfWeek)
            {
                for (var halfHour = 0; halfHour < scheduleTable.DatesByHalfHourByDay[dayOfWeek].Count; halfHour++)
                {
                    if (scheduleTable.DatesByHalfHourByDay[dayOfWeek][halfHour].Count < 2)
                    {
                        // Skip half hours without overlaps
                        continue;
                    }

                    // hourData contains courses which may overlap
                    // so iterate over each pair of them and count the number of overlapping
                    // dates
                    foreach (var dateInHalfHour in scheduleTable.DatesByHalfHourByDay[dayOfWeek][halfHour])
                    {
                        foreach (var date in dateInHalfHour.Course.RegularDates.Find(group => group.Key.StructuralEquals(dateInHalfHour)))
                        {
                            var key = date.From.AtMidnight().Ticks;
                            var startHalfHour = (date.From.FloorHalfHour().Hour * 60 + date.From.FloorHalfHour().Minute) / 30;
                            if (!perHourPerDayTable.ContainsKey(key))
                            {
                                perHourPerDayTable[key] = Enumerable.Repeat<ISet<Date>>(null, 48).ToList();
                            }
                            var dayTable = perHourPerDayTable[key];
                            var length = date.LengthInHalfHours();
                            for (var h = startHalfHour; h < startHalfHour + length; h++)
                            {
                                if (dayTable[h] == null)
                                {
                                    dayTable[h] = new HashSet<Date>();
                                }
                                dayTable[h].Add(date);
                            }
                        }
                    }
                }
            }

            foreach (var dayTable in perHourPerDayTable.Values)
            {
                foreach (var halfHourData in dayTable.Where(it => it != null))
                {
                    foreach (var date in halfHourData)
                    {
                        if (!result.ContainsKey(date))
                        {
                            result[date] = new HashSet<Date>();
                        }
                        result[date].UnionWith(halfHourData.Except(new[] { date }));
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
            List<DayOfWeek> changedDaysOfWeek;
            lock (_changedPendingTutorialsAndCourseUsers)
            {
                var newUsersByCourses = schedule.SelectedCourses.ToDictionary(it => it.Course, it => it.Users.Select(user => user.User.Id).ToList());
                _changedPendingTutorialsAndCourseUsers.AddRange(
                    newUsersByCourses.Where(
                                         newUsersByCourse => !_usersByCourses.ContainsKey(newUsersByCourse.Key)
                                         || _usersByCourses[newUsersByCourse.Key].SymmetricDifference(newUsersByCourse.Value).Any()
                                     )
                                     .Select(newUserByCourse => newUserByCourse.Key)
                    );
                _usersByCourses = newUsersByCourses;

                changedDaysOfWeek = _scheduleTable != null
                    ? newScheduleTable.ChangedDays(_scheduleTable, _changedPendingTutorialsAndCourseUsers).ToList()
                    : DaysOfWeek;
                _changedPendingTutorialsAndCourseUsers.Clear();
            }
            _scheduleTable = newScheduleTable;

            EarliestHalfHour = newScheduleTable.EarliestStartHalfHour;
            LatestHalfHour = newScheduleTable.LatestEndHalfHour;
            var datesByHalfHourByDay = newScheduleTable.DatesByHalfHourByDay;
            OverlappingDates = FindOverlappingDates(newScheduleTable);

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

                foreach (var date in datesByHalfHour
                    .SelectMany(dates => dates)
                    .Distinct()
                    .OrderByDescending(DateLengthSelector))
                {
                    isDayEmpty = false;

                    var flooredFrom = date.From.FloorHalfHour();
                    var halfHourComputed = (flooredFrom.Hour * 60 + flooredFrom.Minute) / 30;
                    var lengthInHalfHours = (int)(date.To.CeilHalfHour() - date.From.FloorHalfHour()).TotalMinutes / 30;

                    var maxOverlappingDates = Enumerable.Range(halfHourComputed, lengthInHalfHours).Max(halfHour2 => datesByHalfHour[halfHour2].Count - 1);
                    //for (var halfHour2 = halfHourComputed; halfHour2 < halfHourComputed + lengthInHalfHours; halfHour2++)
                    //{
                    //    maxOverlappingDates = Math.Max(maxOverlappingDates, datesByHalfHour[halfHour2].Count - 1);
                    //}

                    var course = date.Course;
                    var users = selectedCoursesByCourses.ContainsKey(course) ?
                        selectedCoursesByCourses[course].Users.Select(user => user.User.Name) :
                        Enumerable.Empty<string>();
                    var datesInInterval = course.RegularDates.First(x => Equals(x.Key, date)).ToList();
                    var isPending = allPendingTutorials.Contains(course);
                    var overlapsWithNonPending = OverlapsWithNonPending(OverlappingDates, date, allPendingTutorials);
                    var discourageSelection = course.IsTutorial && isPending && overlapsWithNonPending > 0;
                    var showDisplayTutorials = !course.IsTutorial &&
                        course.AllTutorials.Count > 0 &&
                        course.AllTutorials.SelectMany(it => it).Any(tutorial => tutorial.RegularDates.Count > 0) &&
                        !course.AllTutorials.All(tutorialGroup =>
                            tutorialGroup.Any(tutorial => allPendingTutorials.Contains(tutorial) || selectedCoursesByCourses.ContainsKey(tutorial))
                        );
                    var tutorialParentCourse = course.FindParent(_scheduleTable.Courses);
                    var parentTutorials = tutorialParentCourse?.Tutorials ?? Enumerable.Empty<Course>();
                    var showAlternativeTutorials = course.IsTutorial && parentTutorials.Count() > 1;
                    var internalCourseId = course.IsTutorial
                        ? tutorialParentCourse.InternalCourseID
                        : course.InternalCourseID;

                    var courseViewModel = new CourseViewModel(
                        course, date, users, lengthInHalfHours, maxOverlappingDates, halfHourComputed,
                        columnsForDates[date], datesInInterval, isPending, discourageSelection,
                        overlapsWithNonPending / (double)datesInInterval.Count,
                        showDisplayTutorials, showAlternativeTutorials, internalCourseId);

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
        public static ScheduleViewModel CreateFrom(Schedule schedule, ErrorReporter errorReporter)
        {
            var vm = new ScheduleViewModel();
            try
            {
                vm.UpdateFrom(schedule);
                return vm;
            }
            catch (Exception e)
            {
                errorReporter.Throw(
                    new ArgumentException("Schedule creation failed. Check if the schedule contains invalid data (e.g. duplicate SelectedCourses).", nameof(schedule), e),
                    UserErrorsViewModel.GenericErrorMessage);

                return null;
            }
        }


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

            public readonly ICollection<Course> Courses;

            public ScheduleTable(int earliestStartHalfHour, int latestEndHalfHour,
                Dictionary<DayOfWeek, IList<ISet<Date>>> datesByHalfHourByDay, ICollection<Course> courses)
            {
                EarliestStartHalfHour = earliestStartHalfHour;
                LatestEndHalfHour = latestEndHalfHour;
                DatesByHalfHourByDay = datesByHalfHourByDay;
                Courses = courses;
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
                var addedOrRemovedCourses = Courses.SymmetricDifference(scheduleTable.Courses).ToImmutableHashSet();

                return pendingChangesDifference.Concat(addedOrRemovedCourses)
                    .Concat(scheduleTable.Courses.Union(Courses)
                            .Where(course => course.AllTutorials.SelectMany(it => it).Any(tutorial => addedOrRemovedCourses.Contains(tutorial)))
                    )
                    .SelectMany(course => course.RegularDates)
                    .Select(regularDate => regularDate.Key.From.DayOfWeek)
                    .Distinct();
            }
        }

        public class Weekday
        {
            private static readonly CultureInfo _cultureInfo = new CultureInfo("de-DE");

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
    }
}
