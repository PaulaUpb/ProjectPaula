using System.Linq;

namespace ProjectPaula.Model.CalendarExport
{
    public class ScheduleExporter
    {
        public static string ExportSchedule(Schedule schedule, string username)
        {
            var user = schedule.Users.First(u => u.Name == username);
            var courses = user.SelectedCourses.Where(s => s.SelectedCourse.ScheduleId == schedule.Id).Select(s => s.SelectedCourse.Course);
            var dates = courses.SelectMany(c => c.Dates.Select(d => new CalendarEvent() { StartTime = d.From, EndTime = d.To, Location = d.Room, Name = c.Name, Organizer = d.Instructor }));
            return iCalendar.CreateCalendar(dates);
        }

    }
}
