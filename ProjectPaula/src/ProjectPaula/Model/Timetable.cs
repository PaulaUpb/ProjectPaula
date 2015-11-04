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

            CoursesByDay = new Dictionary<DayOfWeek, List<MockCourse>>
            {
                [DayOfWeek.Monday] = new List<MockCourse> { stabhochsprung },
                [DayOfWeek.Tuesday] = new List<MockCourse> { eidkfk },
                [DayOfWeek.Wednesday] = new List<MockCourse> { gdk }
            };
        }

        public MockCourse GetCourseAt(DayOfWeek dayOfWeek, int halfHour)
        {
            var timeToFind = EarliestTime.AddMinutes(30 * halfHour);

            if (CoursesByDay.ContainsKey(dayOfWeek))
            {
                return CoursesByDay[dayOfWeek].Find(course => course.Begin.Hour == timeToFind.Hour && course.Begin.Minute == timeToFind.Minute);
            }
            return null;
        }
    }


    public class MockCourse
    {
        public string Title { get; set; }

        public DateTime Begin { get; set; }

        public DateTime End { get; set; }

        public int LengthInHalfHours => ((int)(End - Begin).TotalMinutes) / 30;
    }
}
