using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ProjectPaula.Model
{
    public class Timetable
    {
        public Dictionary<DayOfWeek, ISet<Date>> CoursesByDay { get; } = new Dictionary<DayOfWeek, ISet<Date>>(7)
        {
            { DayOfWeek.Monday, new HashSet<Date>()},
          {  DayOfWeek.Tuesday, new HashSet<Date>()},
         {   DayOfWeek.Wednesday, new HashSet<Date>()},
         {   DayOfWeek.Thursday, new HashSet<Date>()},
          {  DayOfWeek.Friday, new HashSet<Date>()},
          {  DayOfWeek.Saturday, new HashSet<Date>()},
         {   DayOfWeek.Sunday, new HashSet<Date>()}
        };

        public ISet<Course> Courses { get; } = new HashSet<Course>();

        public DateTime EarliestTime { get; private set; }

        public DateTime LatestTime { get; private set; }

        public int HalfHourCount { get; private set; }

        public IEnumerable<DateTime> HalfHourTimes { get; private set; }

        public Timetable()
        {
            RecalculateTimes();
        }


        private void RecalculateTimes()
        {
            DateTime? newEarliestTime = null;
            DateTime? newLatestTime = null;

            foreach (var date in CoursesByDay.Values.SelectMany(dates => dates))
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

        public void AddCourse(Course course)
        {
            Courses.Add(course);
            foreach (var regularDate in course.RegularDates.Select(group => group.Key))
            {
                CoursesByDay[regularDate.From.DayOfWeek].Add(regularDate);
            }
            RecalculateTimes();
        }

        public void RemoveCourse(Course course)
        {
            Courses.Remove(course);
            foreach (var regularDate in course.RegularDates.Select(group => group.Key))
            {
                CoursesByDay[regularDate.From.DayOfWeek].Remove(regularDate);
            }
            RecalculateTimes();
        }
    }

}
