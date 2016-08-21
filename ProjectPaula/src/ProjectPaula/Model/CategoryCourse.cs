namespace ProjectPaula.Model
{
    public class CategoryCourse
    {
        public string CourseId { get; set; }
        public int CategoryFilterId { get; set; }

        public virtual Course Course { get; set; }

        public virtual CategoryFilter Category { get; set; }

    }
}
