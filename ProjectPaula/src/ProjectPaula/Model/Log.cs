using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectPaula.Model
{
    public class Log
    {
        public long Id { get; set; }

        public FatilityLevel Level { get; set; }
        public string Tag { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }
    }

    public enum FatilityLevel
    {
        Normal, Error, Critical
    }
}
