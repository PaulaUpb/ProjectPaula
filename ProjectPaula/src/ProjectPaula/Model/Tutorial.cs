﻿using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ProjectPaula.Model
{
    public class Tutorial
    {
        public Tutorial()
        {
            Dates = new List<Date>();
        }

        [JsonIgnore]
        public int Id { get; set; }
        [JsonIgnore]
        public string Url { get; set; }

        public virtual Course Course { get; set; }
        public virtual List<Date> Dates { get; set; }
        [NotMapped]
        public virtual List<IGrouping<Date, Date>> RegularDates { get { return Dates.GroupBy(d => d, new DateComparer()).ToList(); } }
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Tutorial && (obj as Tutorial).Name == Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
