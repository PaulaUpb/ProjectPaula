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
        public Dictionary<DayOfWeek, List<MockCourse>> CoursesByDay { get; private set; }

        public DateTime EarliestTime { get; private set; }

        public DateTime LatestTime { get; private set; }

        public int HalfHourCount { get; private set; }

        public IEnumerable<DateTime> HalfHourTimes()
        {
            for (var time = EarliestTime; time < LatestTime; time = time.AddMinutes(30))
            {
                yield return time;
            }
        }

        public Timetable()
        {
            var now = DateTime.Now;
            EarliestTime = new DateTime(now.Year, now.Month, now.Day, 7, 0, 0);
            LatestTime = new DateTime(now.Year, now.Month, now.Day, 18, 0, 0);
            HalfHourCount = ((int)(LatestTime - EarliestTime).TotalMinutes) / 30;
            var t = HalfHourTimes().Count();
            Debug.Assert(HalfHourCount == HalfHourTimes().Count());

            var stabhochsprung = new MockCourse()
            {
                Begin = EarliestTime,
                End = EarliestTime.AddHours(2),
                Title = "Stabhochsprung für Informatiker"
            };

            var eidkfk = new MockCourse()
            {
                Begin = EarliestTime.AddDays(1).AddHours(1),
                End = EarliestTime.AddDays(1).AddHours(3),
                Title = "Einführung in die Komplexitätstheorie für Kulturwissenschaftler"
            };

            var gdk = new MockCourse()
            {
                Begin = EarliestTime.AddDays(2).AddMinutes(30),
                End = EarliestTime.AddDays(2).AddMinutes(30).AddHours(1),
                Title = "Grundlagen der Kernspaltung"
            };

            var gdk2 = new MockCourse()
            {
                Begin = EarliestTime.AddDays(2).AddMinutes(60),
                End = EarliestTime.AddDays(2).AddMinutes(60).AddHours(1),
                Title = "Grundlagen der Kernspaltung 2"
            };

            var gdk3 = new MockCourse()
            {
                Begin = EarliestTime.AddDays(2).AddMinutes(90),
                End = EarliestTime.AddDays(2).AddMinutes(90).AddHours(1),
                Title = "Grundlagen der Kernspaltung 2"
            };

            CoursesByDay = new Dictionary<DayOfWeek, List<MockCourse>>
            {
                [DayOfWeek.Monday] = new List<MockCourse> { stabhochsprung },
                [DayOfWeek.Tuesday] = new List<MockCourse> { eidkfk },
                [DayOfWeek.Wednesday] = new List<MockCourse> { gdk, gdk2, gdk3 }
            };
        }

        public MultiCourse GetCoursesAt(DayOfWeek dayOfWeek, int halfHour)
        {
            var timeToFind = EarliestTime.AddMinutes(30 * halfHour);

            if (!CoursesByDay.ContainsKey(dayOfWeek))
            {
                return null;
            }

            var courses = CoursesByDay[dayOfWeek];
            var startingCourse = courses.Find(course => course.Begin.Hour == timeToFind.Hour && course.Begin.Minute == timeToFind.Minute);
            if (startingCourse == null)
            {
                return null;
            }

            // We've found a matching course, now find overlapping courses
            var coursesInFoundCourseInterval = new List<MockCourse> { startingCourse };
            for (var i = 1; i < halfHour + startingCourse.LengthInHalfHours; i++)
            {
                var overlappingTimeToFind = timeToFind.AddMinutes(i * 30);
                var overlappingCourse =
                    courses.Find(
                        course => course.Begin.Hour == overlappingTimeToFind.Hour && course.Begin.Minute == overlappingTimeToFind.Minute);
                if (overlappingCourse != null)
                {
                    coursesInFoundCourseInterval.Add(overlappingCourse);
                }
            }

            return new MultiCourse()
            {
                Courses = coursesInFoundCourseInterval
            };
        }
    }


    public class MockCourse
    {
        public string Title { get; set; }

        public DateTime Begin { get; set; }

        public DateTime End { get; set; }

        public int LengthInHalfHours => ((int)(End - Begin).TotalMinutes) / 30;
    }

    public class MultiCourse
    {
        public List<MockCourse> Courses { get; set; }

        public DateTime Begin => Courses.Select(c => c.Begin).Min();

        public DateTime End => Courses.Select(c => c.End).Max();

        public int LengthInHalfHours => ((int)(End - Begin).TotalMinutes) / 30;

        public int HalfHourOffset(MockCourse course) => ((int)(course.Begin - Begin).TotalMinutes) / 30;
    }
}
