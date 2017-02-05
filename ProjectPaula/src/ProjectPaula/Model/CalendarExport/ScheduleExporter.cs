using System.Linq;

namespace ProjectPaula.Model.CalendarExport
{
    public class ScheduleExporter
    {
        public static string ExportSchedule(Schedule schedule, string username)
        {
            var user = schedule.Users.First(u => u.Name == username);

            var courses = user.SelectedCourses
                .Where(s => s.SelectedCourse.ScheduleId == schedule.Id)
                .Select(s => s.SelectedCourse.Course);

            var courseDates = courses.SelectMany(c =>
                c.Dates.Select(d => new CalendarEvent
                {
                    StartTime = d.From,
                    EndTime = d.To,
                    Location = d.Room,
                    Name = c.Name,
                    Organizer = d.Instructor,
                    Uid = $"{c.InternalCourseID}-{d.From.ToString("yyyyMMdd-hhmm")}@paula-upb.de"
                }));

            var examDates = courses.SelectMany(c =>
                c.ExamDates.Select(d => new CalendarEvent
                {
                    StartTime = d.From,
                    EndTime = d.To,
                    Name = d.Description,
                    Location = "",
                    Organizer = d.Instructor,
                    Uid = $"{c.InternalCourseID}-{d.From.ToString("yyyyMMdd-hhmm")}@paula-upb.de"
                }));

            var allDates = courseDates.Concat(examDates).ToList();
            return iCalendar.CreateCalendar(allDates);
        }

    }
}
