using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ProjectPaula.Model
{
    public class Schedule
    {
        public Schedule()
        {
            RecalculateTimes();
        }

        public int Id { get; set; }

        /// <summary>
        /// List of users participating in this Schedule
        /// </summary>
        public virtual List<User> User { get; set; } = new List<User>();

        public virtual List<SelectedCourse> SelectedCourses { get; } = new List<SelectedCourse>();

        /** Cached properties to be recalculated when the courses change **/

        /// <summary>
        /// The set of course dates mapped by the day they're occuring
        /// </summary>
        [NotMapped]
        [JsonIgnore]
        public Dictionary<DayOfWeek, ISet<Date>> DatesByDay { get; } = new Dictionary<DayOfWeek, ISet<Date>>(7)
        {
            { DayOfWeek.Monday, new HashSet<Date>()},
            { DayOfWeek.Tuesday, new HashSet<Date>()},
            { DayOfWeek.Wednesday, new HashSet<Date>()},
            { DayOfWeek.Thursday, new HashSet<Date>()},
            { DayOfWeek.Friday, new HashSet<Date>()},
            { DayOfWeek.Saturday, new HashSet<Date>()},
            { DayOfWeek.Sunday, new HashSet<Date>()}
        };

        /// <summary>
        /// The earlierst hour in this schedule
        /// </summary>
        [NotMapped]
        public DateTime EarliestTime { get; private set; }

        /// <summary>
        /// The latest hour in this schedule
        /// </summary>
        [NotMapped]
        public DateTime LatestTime { get; private set; }

        /// <summary>
        /// The time between the earliest and latest hour divided by 30 minutes.
        /// </summary>
        [NotMapped]
        public int HalfHourCount { get; private set; }

        /// <summary>
        /// EarliestTime, ..., 15:00, 15:30, ..., LatestTime
        /// </summary>
        [NotMapped]
        public IEnumerable<DateTime> HalfHourTimes { get; private set; }

        /** Cached properties to be recalculated when the courses change **/

        /// <summary>
        /// Recompute the <see cref="DatesByDay"/> property.
        /// </summary>
        public void RecalculateDatesByDay()
        {
            foreach (var dates in DatesByDay.Select(x => x.Value))
            {
                dates.Clear();
            }
            foreach (var regularDate in SelectedCourses.SelectMany(x => x.Course.RegularDates).Select(group => group.Key))
            {
                DatesByDay[regularDate.From.DayOfWeek].Add(regularDate);
            }
        }

        /// <summary>
        /// Recompute the properties <see cref="EarliestTime"/>, <see cref="LatestTime"/>, <see cref="HalfHourCount"/>, <see cref="HalfHourTimes"/>
        /// </summary>
        public void RecalculateTimes()
        {
            DateTime? newEarliestTime = null;
            DateTime? newLatestTime = null;

            foreach (var date in DatesByDay.Values.SelectMany(dates => dates))
            {
                if (newEarliestTime == null || newEarliestTime.Value > date.From.AtDate(newEarliestTime.Value.Day, newEarliestTime.Value.Month, newEarliestTime.Value.Year))
                {
                    newEarliestTime = date.From.FloorHalfHour();
                }

                if (newLatestTime == null || newLatestTime.Value < date.To.AtDate(newLatestTime.Value.Day, newLatestTime.Value.Month, newLatestTime.Value.Year))
                {
                    newLatestTime = date.To.CeilHalfHour();
                }
            }

            // Add 2 hours padding as well
            var now = DateTime.Now;
            EarliestTime = (newEarliestTime?.AtDate(now.Day, now.Month, now.Year) ?? new DateTime(now.Year, now.Month, now.Day, 9, 0, 0)).AddHours(-2);
            LatestTime = (newLatestTime?.AtDate(now.Day, now.Month, now.Year) ?? new DateTime(now.Year, now.Month, now.Day, 18, 0, 0)).AddHours(2);
            HalfHourCount = ((int)(LatestTime - EarliestTime).TotalMinutes) / 30;

            var newHalfHourTimes = new List<DateTime>(HalfHourCount);
            for (var time = EarliestTime; time < LatestTime; time = time.AddMinutes(30))
            {
                newHalfHourTimes.Add(time);
            }
            HalfHourTimes = newHalfHourTimes;
        }

        /// <summary>
        /// Add a course to this schedule. This automatically updates the <see cref="DatesByDay"/> and calls <see cref="RecalculateTimes"/>.
        /// </summary>
        /// <param name="selectedCourse"></param>
        public void AddCourse(SelectedCourse selectedCourse)
        {
            SelectedCourses.Add(selectedCourse);
            foreach (var regularDate in selectedCourse.Course.RegularDates.Select(group => group.Key))
            {
                DatesByDay[regularDate.From.DayOfWeek].Add(regularDate);
            }
            RecalculateTimes();
        }

        /// <summary>
        /// Remove a course from this schedule. This automatically updates the <see cref="DatesByDay"/> and calls <see cref="RecalculateTimes"/>.
        /// </summary>
        /// <param name="selectedCourse"></param>
        public void RemoveCourse(SelectedCourse selectedCourse)
        {
            SelectedCourses.Remove(selectedCourse);
            foreach (var regularDate in selectedCourse.Course.RegularDates.Select(group => group.Key))
            {
                DatesByDay[regularDate.From.DayOfWeek].Remove(regularDate);
            }
            RecalculateTimes();
        }

        /// <summary>
        /// Remove a course from this schedule. This automatically updates the <see cref="DatesByDay"/> and calls <see cref="RecalculateTimes"/>.
        /// </summary>
        /// <param name="selectedCourse"></param>
        public void RemoveCourse(string courseId)
        {
            var course = SelectedCourses.FirstOrDefault(selectedCourse => selectedCourse.CourseId == courseId);
            RemoveCourse(course);
        }
    }
}
