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
        private Schedule _schedule;
        private const int searchResultCount = 20;

        public CourseSearchViewModel(CourseCatalog catalog, Schedule schedule)
        {
            _catalog = catalog;
            _schedule = schedule;
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

            var results = PaulRepository.SearchCourses(SearchQuery, _catalog).Take(searchResultCount);
            SearchResults.Clear();
            SearchResults.AddRange(results.Select(o =>
            {
                var added = _schedule.SelectedCourses.Any(s => s.CourseId == o.Id);
                return new SearchResultViewModel(o, added);
            }));
        }
    }

    public class SearchResultViewModel : BindableBase
    {
        public CourseViewModel MainCourse { get; }

        public IReadOnlyCollection<CourseViewModel> ConnectedCourses { get; }

        public SearchResultViewModel(Course course, bool isAdded)
        {
            MainCourse = new CourseViewModel(course, isAdded);
            ConnectedCourses = course.ConnectedCourses.Select(o => new CourseViewModel(o, isAdded)).ToArray();
        }
    }

    public class CourseViewModel : BindableBase
    {
        private Course _course;
        private bool _isAdded;

        public string Name => _course.Name;

        public string Id => _course.Id;

        public string Time { get; }

        public string ShortName => _course.ShortName;

        public string Docent => _course.Docent;

        public bool IsAdded
        {
            get { return _isAdded; }
            set { Set(ref _isAdded, value); }
        }

        public CourseViewModel(Course course, bool isAdded)
        {
            _course = course;
            _isAdded = isAdded;
            Time = string.Join(", ", _course.RegularDates
                .Select(regularDate => regularDate.Key)
                .Select(date => $"{date.From.ToString("ddd HH:mm")} - {date.To.ToString("HH:mm")}"));
        }
    }
}
