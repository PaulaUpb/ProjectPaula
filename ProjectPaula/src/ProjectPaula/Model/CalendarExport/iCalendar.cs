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
        public static string CreateCalendar(IEnumerable<Tuple<DateTimeOffset, DateTimeOffset, string, string, string>> dates)
        {
            using (var db = new DatabaseContext())
            {
                StringBuilder str = new StringBuilder();

                str.AppendLine("BEGIN:VCALENDAR");
                str.AppendLine("VERSION:2.0");
                try
                {
                    foreach (var tuple in dates)
                    {
                        str.AppendLine("BEGIN:VEVENT");
                        str.AppendLine(string.Format("DTSTART;TZID=Europe/Berlin:{0:yyyyMMddTHHmmss}", tuple.Item1));
                        str.AppendLine(string.Format("DTSTAMP;TZID=Europe/Berlin:{0:yyyyMMddTHHmmss}", DateTime.UtcNow));
                        str.AppendLine(string.Format("DTEND;TZID=Europe/Berlin:{0:yyyyMMddTHHmmss}", tuple.Item2));
                        str.AppendLine(string.Format("LOCATION:{0}", tuple.Item3));
                        str.AppendLine(string.Format("UID:{0}", Guid.NewGuid()));
                        str.AppendLine(string.Format("SUMMARY:{0}", tuple.Item4));
                        str.AppendLine(string.Format("DESCRIPTION:{0}", tuple.Item5));
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
}
