using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ProjectPaula.Model
{
    public class Timetable
    {

        private List<Course> _courses;
        public List<Course> Courses
        {
            get { return _courses; }
            set
            {
                _courses = value;
                UpdateValues();
            }
        }

        private void UpdateValues()
        {
            EarliestTime = Courses.SelectMany(course => course.RegularDates)
                .Select(regularDate => regularDate.From)
                .MinBy(from => from.Hour);
            LatestTime = Courses.SelectMany(course => course.RegularDates)
                .Select(regularDate => regularDate.To)
                .MaxBy(to => to.Hour);
            HasCoursesOnSaturday = Courses
                .SelectMany(course => course.RegularDates)
                .Select(regularDate => regularDate.From)
                .Count(from => from.DayOfWeek == DayOfWeek.Saturday) > 0;
            HasCoursesOnSunday = Courses
               .SelectMany(course => course.RegularDates)
               .Select(regularDate => regularDate.From)
               .Count(from => from.DayOfWeek == DayOfWeek.Sunday) > 0;

            DatesByHalfHourTimes = Courses.SelectMany(course => course.RegularDates)
                .GroupBy(regularDate => regularDate.From.FloorHalfHour())
                .ToImmutableDictionary(group => group.Key, group => group.ToList());
        }

        public DateTime EarliestTime { get; private set; }

        public DateTime LatestTime { get; private set; }

        public bool HasCoursesOnSaturday { get; private set; }

        public bool HasCoursesOnSunday { get; private set; }

        public ImmutableDictionary<DateTime, List<Date>> DatesByHalfHourTimes { get; private set; }
    }
}
