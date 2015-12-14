using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectPaula.Model
{
    [JsonObject(IsReference = true)]
    public class User
    {
        public User()
        {
            SelectedCourses = new List<SelectedCourseUser>();
        }
        public int Id { get; set; }

        public string Name { get; set; }
        [JsonIgnore]
        public virtual List<SelectedCourseUser> SelectedCourses { get; set; }

        public string ScheduleId { get; set; }
    }
}
