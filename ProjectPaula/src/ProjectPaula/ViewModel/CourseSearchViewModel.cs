using ProjectPaula.DAL;
using ProjectPaula.Model;
using ProjectPaula.Model.ObjectSynchronization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ProjectPaula.ViewModel
{
    /// <summary>
    /// Executes course search queries and provides
    /// a list of search results.
    /// </summary>
    public class CourseSearchViewModel : BindableBase
    {
        private CourseCatalog _catalog;

        public CourseSearchViewModel(CourseCatalog catalog)
        {
            _catalog = catalog;
        }

        private string _searchQuery;

        public string SearchQuery
        {
            get { return _searchQuery; }
            set
            {
                if (Set(ref _searchQuery, value))
                    UpdateSearchResults();
            }
        }

        public ObservableCollectionEx<SearchResultViewModel> SearchResults { get; } = new ObservableCollectionEx<SearchResultViewModel>();

        private void UpdateSearchResults()
        {
            if (SearchQuery == null || SearchQuery.Count() < 3)
                return;

            var results = PaulRepository.SearchCourses(SearchQuery, _catalog);
            SearchResults.Clear();
            SearchResults.AddRange(results.Select(o => new SearchResultViewModel(o)));
        }
    }

    public class SearchResultViewModel
    {
        public CourseViewModel MainCourse { get; }

        public IReadOnlyCollection<CourseViewModel> ConnectedCourses { get; }

        public SearchResultViewModel(Course course)
        {
            MainCourse = new CourseViewModel(course);
            ConnectedCourses = course.ConnectedCourses.Select(o => new CourseViewModel(o)).ToArray();
        }
    }

    public class CourseViewModel
    {
        private Course _course;

        public string Name => _course.Name;

        public string Id => _course.Id;

        public string Time { get; }

        public string ShortName => _course.ShortName;

        public CourseViewModel(Course course)
        {
            _course = course;

            Time = string.Join(", ", _course.RegularDates
                .Select(regularDate => regularDate.Key)
                .Select(date => $"{date.From.ToString("ddd HH:mm")} - {date.To.ToString("HH:mm")}"));
        }
    }
}
