using ProjectPaula.DAL;
using ProjectPaula.Model;
using ProjectPaula.Model.ObjectSynchronization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectPaula.ViewModel
{
    /// <summary>
    /// Contains properties that are shared across all connected clients.
    /// This ViewModel is available during the whole SignalR session.
    /// </summary>
    public class SharedPublicViewModel : BindableBase
    {
        private int _clientCount;
        private CourseCatalog[] _availableSemesters;

        public int ClientCount
        {
            get { return _clientCount; }
            set { Set(ref _clientCount, value); }
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
            => AvailableSemesters = await PaulRepository.GetCourseCataloguesAsync();
    }
}
