using ProjectPaula.DAL;
using ProjectPaula.Model;
using ProjectPaula.Model.ObjectSynchronization;
using ProjectPaula.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
            set { Set(ref _searchQuery, value); }
        }

        public ObservableCollectionEx<SearchResultViewModel> SearchResults { get; } = new ObservableCollectionEx<SearchResultViewModel>();

        public void UpdateSearchResults(ErrorReporter errorReporter)
        {
            if (SearchQuery == null || SearchQuery.Count() < 3)
            {
                errorReporter.Throw(
                    new InvalidOperationException("Search query is null or too short"),
                    UserErrorsViewModel.SearchQueryTooShortMessage);
            }

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
        public SearchResultCourseViewModel MainCourse { get; }

        public IReadOnlyCollection<SearchResultCourseViewModel> ConnectedCourses { get; }

        public SearchResultViewModel(Course course, bool isAdded)
        {
            MainCourse = new SearchResultCourseViewModel(course, isAdded);
            ConnectedCourses = course.ConnectedCourses.Select(o => new SearchResultCourseViewModel(o, isAdded)).ToArray();
        }
    }

    public class SearchResultCourseViewModel : BindableBase
    {
        private Course _course;
        private bool _isAdded;

        public string Name => _course.Name;

        public string Id => _course.Id;

        public string Time { get; }

        public int TutorialCount => _course.Tutorials.Count;

        public string ShortName => _course.ShortName;

        public string Docent => _course.Docent;

        public bool IsAdded
        {
            get { return _isAdded; }
            set { Set(ref _isAdded, value); }
        }

        public SearchResultCourseViewModel(Course course, bool isAdded)
        {
            _course = course;
            _isAdded = isAdded;
            Time = string.Join(", ", _course.RegularDates
                .Select(regularDate => regularDate.Key)
                .Select(date => $"{date.From.ToString("ddd HH:mm", new CultureInfo("de-DE"))} - {date.To.ToString("HH:mm", new CultureInfo("de-DE"))}"));
        }
    }
}
