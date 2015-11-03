using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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
        [NotMapped]
        public virtual List<IGrouping<Date, Date>> RegularDates { get { return Dates.GroupBy(d => d, new DateComparer()).ToList(); } }
        public string Name { get; set; }
    }
}
