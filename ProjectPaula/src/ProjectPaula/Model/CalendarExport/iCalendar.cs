using ProjectPaula.DAL;
using System;
using System.Collections.Generic;
using System.Text;

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
        public static string CreateCalendar(IEnumerable<CalendarEvent> dates)
        {
            using (var db = new DatabaseContext(PaulRepository.Filename))
            {
                StringBuilder str = new StringBuilder();

                str.AppendLine("BEGIN:VCALENDAR");
                str.AppendLine("X-PUBLISHED-TTL:PT6H");
                str.AppendLine("VERSION:2.0");
                try
                {
                    foreach (var d in dates)
                    {
                        str.AppendLine("BEGIN:VEVENT");
                        str.AppendLine(string.Format("DTSTART:{0:yyyyMMddTHHmmssZ}", d.StartTime.ToUniversalTime()));
                        str.AppendLine(string.Format("DTSTAMP:{0:yyyyMMddTHHmmssZ}", DateTime.UtcNow));
                        str.AppendLine(string.Format("DTEND:{0:yyyyMMddTHHmmssZ}", d.EndTime.ToUniversalTime()));
                        str.AppendLine(string.Format("LOCATION:{0}", d.Location));
                        str.AppendLine(string.Format("UID:{0}", Guid.NewGuid()));
                        str.AppendLine(string.Format("SUMMARY:{0}", d.Name));
                        str.AppendLine(string.Format("DESCRIPTION:{0}", d.Organizer));
                        str.AppendLine("END:VEVENT");
                    }

                }
                catch (Exception e)
                {
                    db.Logs.Add(new Log() { Message = "Exception at time conversion" + e.Message, Date = DateTime.Now });
                    db.SaveChanges();
                }
                str.AppendLine("END:VCALENDAR");
                return str.ToString();
            }
        }


    }
    public struct CalendarEvent
    {
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }

        public string Location { get; set; }

        public string Name { get; set; }

        public string Organizer { get; set; }
    }

}
