using System;

namespace ProjectPaula.Model.Public
{
    public class PublicDate
    {
        public DateTimeOffset From;

        public DateTimeOffset To;

        public string Room;

        public string Instructor;

        public PublicDate(Date date)
        {
            From = date.From;
            To = date.To;
            Room = date.Room;
            Instructor = date.Instructor;
        }

        public PublicDate(ExamDate date)
        {
            From = date.From;
            To = date.To;
            Room = "";
            Instructor = date.Instructor;
        }
    }
}
