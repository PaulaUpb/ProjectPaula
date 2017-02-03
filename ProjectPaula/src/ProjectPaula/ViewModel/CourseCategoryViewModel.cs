using ProjectPaula.DAL;
using ProjectPaula.Model;
using ProjectPaula.Model.ObjectSynchronization;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;
using Newtonsoft.Json;
using ProjectPaula.Model.ObjectSynchronization.ChangeTracking;
using System.Collections.ObjectModel;

namespace ProjectPaula.ViewModel
{
    /// <summary>
    /// Represents a single node in the course catalog browsing feature.
    /// </summary>
    public class CourseCategoryViewModel : BindableBase
    {
        private readonly CourseCatalog _catalog;
        private readonly CategoryFilter _category;
        private bool _isLoaded;

        public string Title => _category?.Title ?? "Kurskatalog";

        /// <summary>
        /// Category ID. Used by the client to specify which node in the tree to load.
        /// </summary>
        public int ID => _category?.ID ?? -1;

        public bool IsLoaded
        {
            get { return _isLoaded; }
            private set { Set(ref _isLoaded, value); }
        }

        public ObservableCollectionEx<CourseCategoryViewModel> Subcategories { get; } =
            new ObservableCollectionEx<CourseCategoryViewModel>();

        public ObservableCollectionEx<SearchResultViewModel> Courses { get; } =
            new ObservableCollectionEx<SearchResultViewModel>();

        /// <summary>
        /// Creates the root node of the catalog browsing tree.
        /// </summary>
        public CourseCategoryViewModel(CourseCatalog catalog)
        {
            _catalog = catalog;

            Subcategories.AddRange(PaulRepository.CategoryFilter
                .Where(category => category.CourseCatalog.InternalID == catalog.InternalID && category.IsTopLevel)
                .OrderBy(category => category.Title)
                .Select(category => new CourseCategoryViewModel(category))
                .ToList());

            IsLoaded = true;
        }

        /// <summary>
        /// Creates an inner node of the catalog browsing tree.
        /// </summary>
        /// <param name="category"></param>
        public CourseCategoryViewModel(CategoryFilter category)
        {
            _category = category;
        }

        public void Load(Schedule schedule)
        {
            if (_category == null || IsLoaded)
                return; // children are already loaded for root nodes

            Subcategories.AddRange(_category.Subcategories
                .OrderBy(cat => cat.Title)
                .Select(cat => new CourseCategoryViewModel(cat))
                .ToList());

            Courses.AddRange(_category.Courses
                .OrderBy(cc => cc.Course.Name)
                .Select(cc =>
                {
                    var added = schedule.SelectedCourses.Any(s => s.CourseId == cc.CourseId);
                    return new SearchResultViewModel(cc.Course, added);
                })
                .ToList());

            IsLoaded = true;
        }

        [JsonIgnore, DoNotTrack]
        public IEnumerable<CourseCategoryViewModel> DescendantsAndSelf
        {
            get
            {
                var queue = new Queue<CourseCategoryViewModel>();
                queue.Enqueue(this);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    yield return current;

                    if (current.IsLoaded)
                    {
                        foreach (var child in current.Subcategories)
                            queue.Enqueue(child);
                    }
                }
            }
        }
    }
}
