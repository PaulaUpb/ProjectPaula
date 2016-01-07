using Newtonsoft.Json;
using System.Collections.Generic;

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
