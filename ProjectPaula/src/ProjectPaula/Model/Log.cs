using System;

namespace ProjectPaula.Model
{
    public class Log
    {
        public long Id { get; set; }

        public FatalityLevel Level { get; set; }
        public string Tag { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }
    }

    public enum FatalityLevel
    {
        Verbose,
        Normal,
        Error,
        Critical
    }
}