using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectPaula.Model.CalendarExport
{
    public class ScheduleExporter
    {
        public static string ExportSchedule(Schedule schedule)
        {
            var courses = schedule.SelectedCourses.Select(s => s.Course);
            var dates = courses.SelectMany(c => c.Dates.Select(d => new CalendarEvent() { StartTime = d.From, EndTime = d.To, Location = d.Room, Name = c.Name, Organizer = d.Instructor }));
            return iCalendar.CreateCalendar(dates);
        }

    }
}
