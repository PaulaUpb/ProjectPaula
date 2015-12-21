using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using System.IO;
using System.Linq;
using System.Text;

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

        public static void UpdateSchedule(Schedule schedule)
        {
            var basePath = CallContextServiceLocator.Locator.ServiceProvider
                            .GetRequiredService<IApplicationEnvironment>().ApplicationBasePath;

            var filePath = basePath + $"/Calendars/schedule{schedule.Id}.ics";
            if (File.Exists(filePath)) File.Delete(filePath);
            File.WriteAllBytes(filePath, Encoding.UTF8.GetBytes(ExportSchedule(schedule)));

        }
    }
}
