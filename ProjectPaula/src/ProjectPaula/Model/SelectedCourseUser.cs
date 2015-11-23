using Newtonsoft.Json;

namespace ProjectPaula.Model
{
    [JsonObject(IsReference = true)]
    public class SelectedCourseUser
    {
        public virtual User User { get; set; }
        public virtual SelectedCourse SelectedCourse { get; set; }

    }
}