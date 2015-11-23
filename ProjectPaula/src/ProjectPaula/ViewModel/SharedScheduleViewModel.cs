using Newtonsoft.Json;
using ProjectPaula.Model;
using ProjectPaula.Model.ObjectSynchronization;
using System.Collections.ObjectModel;

namespace ProjectPaula.ViewModel
{
    /// <summary>
    /// Contains properties that are shared across all clients
    /// that have joined the same schedule.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class SharedScheduleViewModel : BindableBase
    {
        public Schedule Schedule { get; }

        /// <summary>
        /// Gets a list of users that are connected and have joined
        /// the schedule specified with <see cref="Schedule"/>.
        /// </summary>
        [JsonProperty]
        public ObservableCollection<PaulaClientViewModel> Users { get; } = new ObservableCollection<PaulaClientViewModel>();

        /// <summary>
        /// Gets a list of usernames that are known but not currently
        /// used by any client.
        /// </summary>
        [JsonProperty]
        public ObservableCollection<string> AvailableUserNames { get; } = new ObservableCollection<string>();

        public SharedScheduleViewModel(Schedule schedule)
        {
            Schedule = schedule;

            // For testing purposes only
            AvailableUserNames.Add("Christian");
            AvailableUserNames.Add("Michél");
            AvailableUserNames.Add("Sven");
        }
    }
}
