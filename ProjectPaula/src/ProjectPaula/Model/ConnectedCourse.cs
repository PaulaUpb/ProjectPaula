namespace ProjectPaula.Model
{
    public class ConnectedCourse
    {
        public string CourseId { get; set; }
        public string CourseId2 { get; set; }
        public override bool Equals(object obj)
        {
            return obj is ConnectedCourse && ((ConnectedCourse)obj).CourseId2 == CourseId2 && ((ConnectedCourse)obj).CourseId == CourseId;
        }

        public override int GetHashCode()
        {
            return CourseId2.GetHashCode();
        }
    }


}
