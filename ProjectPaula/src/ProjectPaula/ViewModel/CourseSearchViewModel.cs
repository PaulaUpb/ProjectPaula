using ProjectPaula.DAL;
using ProjectPaula.Model.ObjectSynchronization;
using System.Collections.ObjectModel;
using System.Linq;
using ProjectPaula.Model;

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
            {
                var time = string.Join(", ", result.RegularDates
                    .Select(regularDate => regularDate.Key)
                    .Select(date => $"{date.From.ToString("ddd HH:mm")} - {date.To.ToString("HH:mm")}"));
                SearchResults.Add(new SearchResultViewModel(result.Name, result.Id, time));
            }
        }
    }

    public class SearchResultViewModel
    {
        public string Name { get; }

        public string Id { get; }

        public string Time { get; }

        public SearchResultViewModel(string name, string id, string time)
        {
            Name = name;
            Id = id;
            Time = time;
        }
    }
}
