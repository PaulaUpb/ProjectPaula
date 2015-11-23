using ProjectPaula.DAL;
using ProjectPaula.Model.ObjectSynchronization;
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

        public ObservableCollection<SearchResultViewModel> SearchResults { get; } = new ObservableCollection<SearchResultViewModel>();

        private void UpdateSearchResults()
        {
            if (SearchQuery == null || SearchQuery.Count() < 3)
                return;

            SearchResults.Clear();

            var results = PaulRepository.GetLocalCourses(SearchQuery);

            foreach (var result in results)
                SearchResults.Add(new SearchResultViewModel(result.Name, result.Id));
        }

        public class SearchResultViewModel
        {
            public string Name { get; }

            public string Id { get; }

            public SearchResultViewModel(string name, string id)
            {
                Name = name;
                Id = id;
            }
        }
    }
}
