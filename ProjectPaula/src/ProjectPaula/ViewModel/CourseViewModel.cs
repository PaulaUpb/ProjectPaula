using ProjectPaula.DAL;
using ProjectPaula.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ProjectPaula.ViewModel
{
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
        public IList<string> Users { get; }

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

        public int Column { get; }

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

        public IList<string> AllDates { get; }

        public bool IsTutorial { get; }

        public bool ShowDisplayTutorials { get; }

        public bool ShowAlternativeTutorials { get; }

        /// <summary>
        /// ID of this course in the database.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// An ID which is the same for all connected courses and
        /// tutorials of a course. This "groups together" dates of
        /// the same course and its connected courses and tutorials
        /// so that we can display that they are related.
        /// </summary>
        public string MainCourseId { get; }

        public CourseViewModel(
            Course course, Date date, IEnumerable<string> users, int lengthInHalfHours,
            int overlappingDatesCount, int offsetHalfHourY, int column, IList<Date> dates,
            bool isPending, bool discourageSelection, double overlapsQuote,
            bool showDisplayTutorials, bool showAlternativeTutorials)
        {
            Id = course.Id;
            Title = course.Name;
            IsTutorial = course.IsTutorial;
            Begin = date.From;
            End = date.To;
            LengthInHalfHours = lengthInHalfHours;
            OverlappingDatesCount = overlappingDatesCount;
            OffsetHalfHourY = offsetHalfHourY;
            Column = column;
            IsPending = isPending;
            DiscourageSelection = discourageSelection;
            OverlapsQuote = overlapsQuote;
            ShowDisplayTutorials = showDisplayTutorials;
            ShowAlternativeTutorials = showAlternativeTutorials;
            Users = users.ToList();
            Time = $"{Begin.ToString("t", new CultureInfo("de-DE"))} - {End.ToString("t", new CultureInfo("de-DE"))}, {ComputeIntervalDescription(dates)}";
            AllDates = dates.OrderBy(d => d.From).Select(d => d.From.ToString("dd.MM.yy", new CultureInfo("de-DE"))).ToList();

            // "Group" related course dates by finding their main/parent course
            var currentCourse = course.IsTutorial ?
                course.FindParent(PaulRepository.Courses) :
                course;

            MainCourseId = currentCourse.IsConnectedCourse ?
                MainCourseId = currentCourse.ConnectedCourses.FirstOrDefault(c => !c.IsConnectedCourse)?.Id ?? course.Id :
                MainCourseId = currentCourse.Id;
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
                return $"nur am {dates[0].From.ToString("dd.MM.yy", new CultureInfo("de-DE"))}";
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
