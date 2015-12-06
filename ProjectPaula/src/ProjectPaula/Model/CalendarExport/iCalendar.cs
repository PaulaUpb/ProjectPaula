using ProjectPaula.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPaula.Model.CalendarExport
{
    public class iCalendar
    {
        /// <summary>
        /// Creates an Calendar in the iCal format
        /// </summary>
        /// <param name="dates">A list of dates given by tuples. The parameters in the tuples: StartTime,EndTime,Location,Description,Organizer</param>
        /// <param name="attendees">List of attendees</param>
        /// <returns>Calendar as a string</returns>
        public static string CreateCalendar(IEnumerable<Tuple<DateTime, DateTime, string, string, string>> dates)
        {
            StringBuilder str = new StringBuilder();
            str.AppendLine("BEGIN:VCALENDAR");
            str.AppendLine("VERSION:2.0");
            try
            {
                foreach (var tuple in dates)
                {
                    var time1 = TimeZoneInfo.ConvertTime(tuple.Item1, TimeZoneInfo.Local, TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time")).ToUniversalTime();
                    var time2 = TimeZoneInfo.ConvertTime(tuple.Item2, TimeZoneInfo.Local, TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time")).ToUniversalTime();
                    str.AppendLine("BEGIN:VEVENT");
                    str.AppendLine(string.Format("DTSTART:{0:yyyyMMddTHHmmssZ}", time1));
                    str.AppendLine(string.Format("DTSTAMP:{0:yyyyMMddTHHmmssZ}", DateTime.UtcNow));
                    str.AppendLine(string.Format("DTEND:{0:yyyyMMddTHHmmssZ}", time2));
                    str.AppendLine(string.Format("LOCATION:{0}", tuple.Item3));
                    str.AppendLine(string.Format("UID:{0}", Guid.NewGuid()));
                    str.AppendLine(string.Format("SUMMARY:{0}", tuple.Item4));
                    str.AppendLine(string.Format("DESCRIPTION:{0}", tuple.Item5));
                    str.AppendLine("END:VEVENT");
                }
                str.AppendLine("END:VCALENDAR");

                return str.ToString();
            }
            catch (Exception e)
            {
                using (var db = new DatabaseContext())
                {
                    db.Logs.Add(new Log() { Message = "Exception at time conversion" + e.Message, Date = DateTime.Now });
                    var zones = TimeZoneInfo.GetSystemTimeZones();
                    foreach (var z in zones)
                    {
                        db.Logs.Add(new Log() { Message = "Time zone:" + z.Id, Date = DateTime.Now });
                    }
                    db.SaveChanges();

                }
            }
        }


    }
}
