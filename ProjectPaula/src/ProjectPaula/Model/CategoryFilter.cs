using System.Collections.Generic;

namespace ProjectPaula.Model
{
    public class CategoryFilter
    {
        public CategoryFilter()
        {
            Courses = new List<CategoryCourse>();
            Subcategories = new List<CategoryFilter>();
        }

        public int ID { get; set; }

        public string Title { get; set; }

        public bool IsTopLevel { get; set; }

        public virtual List<CategoryFilter> Subcategories { get; set; }

        public virtual List<CategoryCourse> Courses { get; set; }

        public virtual CourseCatalog CourseCatalog { get; set; }

        public override int GetHashCode()
        {
            return Title.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is CategoryFilter category &&
                category.Title == Title &&
                category.CourseCatalog == CourseCatalog;
        }
    }
}
