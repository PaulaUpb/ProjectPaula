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
        /// The schedule ID. 
        /// TODO: Refer to schedule object's ID
        /// </summary>
        [JsonProperty]
        public string Id { get; }

        /// <summary>
        /// Gets a list of users that are connected and have joined
        /// the schedule specified with <see cref="Schedule"/>.
        /// </summary>
        [JsonProperty]
        public ObservableCollection<UserViewModel> Users { get; } = new ObservableCollection<UserViewModel>();

        /// <summary>
        /// Gets a list of usernames that are known but not currently
        /// used by any client.
        /// </summary>
        [JsonProperty]
        public ObservableCollection<string> AvailableUserNames { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Initializes a new <see cref="SharedScheduleViewModel"/>
        /// with the specified <see cref="Schedule"/>.
        /// </summary>
        /// <param name="schedule">Schedule</param>
        /// <param name="id">Schedule ID (this is temporary, should later be determined by the schedule object itself)</param>
        public SharedScheduleViewModel(Schedule schedule, string id)
        {
            Schedule = schedule;
            Id = id;

            // For testing purposes only
            AvailableUserNames.Add("Christian");
            AvailableUserNames.Add("Michél");
            AvailableUserNames.Add("Sven");
        }
    }
}
