using Newtonsoft.Json;
using ProjectPaula.DAL;
using ProjectPaula.Model;
using ProjectPaula.Model.ObjectSynchronization;
using ProjectPaula.Model.ObjectSynchronization.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ProjectPaula.ViewModel
{
    /// <summary>
    /// Enables browsing the category tree of PAUL course catalogs.
    /// </summary>
    public class CategoryBrowsingViewModel : BindableBase
    {
        private readonly Schedule _schedule;
        private CategoryVM _currentCategory;

        public CategoryVM CurrentCategory
        {
            get => _currentCategory;
            set => Set(ref _currentCategory, value);
        }

        public ObservableCollectionEx<CategoryVM> AncestorCategories { get; } = new ObservableCollectionEx<CategoryVM>();

        public ObservableCollectionEx<CategoryVM> Subcategories { get; } = new ObservableCollectionEx<CategoryVM>();

        public ObservableCollectionEx<SearchResultViewModel> Courses { get; } = new ObservableCollectionEx<SearchResultViewModel>();

        public CategoryBrowsingViewModel(Schedule schedule)
        {
            _schedule = schedule;
            var root = new CategoryVM(schedule.CourseCatalogue);
            CurrentCategory = root;
            LoadSubcategoriesAndCourses();
        }

        public void NavigateToRoot()
        {
            CurrentCategory = AncestorCategories[0];
            AncestorCategories.Clear();
            LoadSubcategoriesAndCourses();
        }

        /// <summary>
        /// Navigates to a category that is either the direct child (= subcategory) of the
        /// current category or any ancestor.
        /// </summary>
        /// <param name="category"></param>
        public void Navigate(CategoryFilter category)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            var childCategory = Subcategories.FirstOrDefault(c => c.ID == category.ID);

            if (childCategory != null)
            {
                // Go to subcategory, adding the current category to the ancestor list
                if (CurrentCategory != null)
                    AncestorCategories.Add(CurrentCategory);
                CurrentCategory = Subcategories.FirstOrDefault(c => c.ID == category.ID);
                LoadSubcategoriesAndCourses();
            }
            else
            {
                // Find the desired category in the ancestor list, set as new current category,
                // and remove all following ancestors
                for (var i = 0; i < AncestorCategories.Count; i++)
                {
                    if (AncestorCategories[i].ID == category.ID)
                    {
                        CurrentCategory = AncestorCategories[i];

                        while (AncestorCategories.Count > i)
                            AncestorCategories.RemoveAt(AncestorCategories.Count - 1);

                        LoadSubcategoriesAndCourses();
                        return;
                    }
                }
            }
        }

        private void LoadSubcategoriesAndCourses()
        {
            Subcategories.Clear();
            Courses.Clear();

            // Load subcategories
            IEnumerable<CategoryFilter> newSubcategories;

            if (CurrentCategory.IsRoot)
            {
                newSubcategories = PaulRepository.CategoryFilter.Where(category =>
                    category.CourseCatalog?.InternalID == _schedule.CourseCatalogue.InternalID && category.IsTopLevel);
            }
            else
            {
                newSubcategories = CurrentCategory.Category.Subcategories;
            }

            Subcategories.AddRange(newSubcategories
                .OrderBy(subcategory => subcategory.Title)
                .Select(subcategory => new CategoryVM(subcategory)));

            // Load courses
            if (!CurrentCategory.IsRoot)
            {
                Courses.AddRange(CurrentCategory.Category.Courses
                    .Select(cc => cc.Course)
                    .Where(course => !course.IsTutorial && (!course.IsConnectedCourse || course.ConnectedCourses.All(c => c.IsConnectedCourse)))
                    .OrderBy(course => course.Name)
                    .Select(course =>
                    {
                        var added = _schedule.SelectedCourses.Any(s => s.CourseId == course.Id);
                        return new SearchResultViewModel(course, added);
                    }));
            }
        }

        public class CategoryVM
        {
            private const int _rootCategoryID = -1;

            [JsonIgnore, DoNotTrack]
            public CategoryFilter Category { get; }

            public string Title { get; }

            public int ID { get; }

            [JsonIgnore, DoNotTrack]
            public bool IsRoot => ID == _rootCategoryID;

            /// <summary>
            /// Creates a fake category representing the root of the tree.
            /// </summary>
            public CategoryVM(CourseCatalog courseCatalog)
            {
                Title = courseCatalog.Title;
                ID = _rootCategoryID;
            }

            public CategoryVM(CategoryFilter category)
            {
                Category = category;
                Title = category.Title;
                ID = category.ID;
            }
        }
    }
}
