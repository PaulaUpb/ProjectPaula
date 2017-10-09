using ProjectPaula.DAL;
using ProjectPaula.Model;
using ProjectPaula.Model.ObjectSynchronization;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProjectPaula.ViewModel
{
    /// <summary>
    /// Contains properties that are shared across all connected clients.
    /// This ViewModel is available during the whole SignalR session.
    /// </summary>
    public class SharedPublicViewModel : BindableBase
    {
        private readonly object _lock = new object();
        private int _clientCount;
        private CourseCatalog[] _availableSemesters;

        public int ClientCount
        {
            get { return _clientCount; }
            set
            {
                lock (_lock)
                {
                    Set(ref _clientCount, value);
                }
            }
        }

        public IEnumerable<CourseCatalog> AvailableSemesters
        {
            get { return _availableSemesters; }
            private set { Set(ref _availableSemesters, value.ToArray()); }
        }

        private SharedPublicViewModel() { }

        public static async Task<SharedPublicViewModel> CreateAsync()
        {
            var vm = new SharedPublicViewModel();
            await vm.RefreshAvailableSemestersAsync();
            return vm;
        }

        public async Task RefreshAvailableSemestersAsync()
        {
            AvailableSemesters = (await PaulRepository.GetCourseCataloguesAsync()).
                Where(c => PaulRepository.Courses.Any(course => course.Catalogue.Equals(c)))
                .OrderByDescending(catalog => GetCourseCatalogOrder(catalog));
        }

        private static string GetCourseCatalogOrder(CourseCatalog catalog)
        {
            var match = Regex.Match(catalog.ShortTitle, @"(WS|SS)\s*([0-9]+)");

            if (match.Success && match.Groups.Count >= 3)
            {
                return match.Groups[2].Value + match.Groups[1].Value;
            }
            else
            {
                return catalog.ShortTitle;
            }
        }
    }
}
