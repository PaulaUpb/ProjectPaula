namespace ProjectPaula.Model
{
    public class SelectedCourseUser
    {
        public int UserId { get; set; }
        public virtual User User { get; set; }
        public int SelectedCourseId { get; set; }
        public virtual SelectedCourse SelectedCourse { get; set; }

    }
}