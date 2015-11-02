using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPaul.Model
{
    public class Tutorial
    {
        public int Id { get; set; }
        public virtual Course Course { get; set; }
        public virtual List<Date> Dates { get; set; }

        public virtual List<Date> RegularDates { get; set; }
        public string Name { get; set; }
    }
}
