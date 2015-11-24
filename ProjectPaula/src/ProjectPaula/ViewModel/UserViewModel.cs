using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ProjectPaula.Model.ObjectSynchronization;
using ProjectPaula.Model.ObjectSynchronization.ChangeTracking;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectPaula.ViewModel
{
    /// <summary>
    /// Represents a connected client identified by a SignalR connection ID.
    /// Stores the current user name and the schedule the client is working on.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class UserViewModel : BindableBase
    {
        private ScheduleManager _scheduleManager;
        private string _name;
        private SessionState _state = SessionState.Default;

        public string ConnectionId { get; }

        [JsonProperty]
        public string Name
        {
            get { return _name; }
            set { Set(ref _name, value); }
        }

        [JsonProperty]
        //[JsonConverter(typeof(StringEnumConverter))]
        public SessionState State
        {
            get { return _state; }
            set { Set(ref _state, value); }
        }

        public CourseSearchViewModel SearchVM { get; private set; }

        /// <summary>
        /// The schedule ViewModel that is shared across all
        /// users who have joined the same schedule.
        /// </summary>
        [DoNotTrack] // Prevents circular references
        public SharedScheduleViewModel SharedScheduleVM { get; private set; }

        /// <summary>
        /// The schedule ViewModel that is specific to this client.
        /// </summary>
        public ScheduleViewModel TailoredScheduleVM { get; private set; }

        public UserViewModel(ScheduleManager scheduleManager, string connectionId)
        {
            _scheduleManager = scheduleManager;
            ConnectionId = connectionId;
        }

        /// <summary>
        /// Depending on the current state this method
        /// removes the user from the schedule he/she has joined
        /// or cancels the join attempt.
        /// </summary>
        public async Task DisconnectAsync()
        {
            switch (State)
            {
                case SessionState.Default:
                    // Nothing to do
                    break;

                case SessionState.JoiningSchedule:
                    SharedScheduleVM = null;
                    break;

                case SessionState.JoinedSchedule:
                    SharedScheduleVM.Users.Remove(this);
                    SharedScheduleVM.AvailableUserNames.Add(Name);
                    await _scheduleManager.SaveScheduleAsync(SharedScheduleVM);
                    SharedScheduleVM = null;
                    TailoredScheduleVM = null;
                    SearchVM = null;
                    Name = null;
                    break;

                default:
                    throw new NotImplementedException();
            }

            State = SessionState.Default;
        }

        /// <summary>
        /// Loads the schedule with the specified ID and assigns it
        /// to the calling client. After this the client is expected
        /// to choose a user name and then call
        /// <see cref="CompleteJoinSchedule(string)"/> to actually join
        /// the schedule and start collaborating with others.
        /// </summary>
        /// <remarks>
        /// After this call, synchronization of <see cref="SharedScheduleVM"/>
        /// with the calling client should start.
        /// </remarks>
        /// <param name="scheduleID">Schedule ID</param>
        public void BeginJoinSchedule(string scheduleID)
        {
            if (SharedScheduleVM != null)
                throw new InvalidOperationException("The client has already joined a schedule");

            // TODO: Handle null/exception
            var scheduleVM = _scheduleManager.GetOrLoadSchedule(scheduleID);

            SharedScheduleVM = scheduleVM;
            State = SessionState.JoiningSchedule;
        }

        /// <summary>
        /// Joins the schedule specified by <see cref="BeginJoinSchedule(string)"/>
        /// by adding the calling client to the schedule's list of users.
        /// </summary>
        /// <remarks>
        /// After this call, synchronization of <see cref="TailoredScheduleVM"/>
        /// with the calling client should start.
        /// </remarks>
        /// <param name="userName">
        /// User name (either one of the schedule's known but currently unused user names
        /// (see <see cref="SharedScheduleViewModel.AvailableUserNames"/> or a new name).
        /// </param>
        public void CompleteJoinSchedule(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentException(nameof(userName));

            if (Name != null)
                throw new InvalidOperationException();

            // Check if user name already in use
            if (SharedScheduleVM.Users.Any(o => o.Name == userName))
                throw new ArgumentException($"The user name '{userName}' is already in use");

            // If user name is known, remove it from the list of available names
            SharedScheduleVM.AvailableUserNames.Remove(userName);

            Name = userName;
            SharedScheduleVM.Users.Add(this);


            // TODO: Properly create TailoredScheduleViewModel
            //       (not yet sure which properties can be shared and which must
            //       be tailored and how to sync changes between both VMs)
            TailoredScheduleVM = ScheduleViewModel.CreateFrom(SharedScheduleVM.Schedule);
            SearchVM = new CourseSearchViewModel();

            State = SessionState.JoinedSchedule;
        }

        /// <summary>
        /// Creates a new schedule with a random identifier
        /// and makes the client join it using the specified user name.
        /// </summary>
        public void CreateSchedule(string userName)
        {
            // TODO
            throw new NotImplementedException();
        }
    }

    public enum SessionState
    {
        /// <summary>
        /// The client is connected.
        /// </summary>
        Default,

        /// <summary>
        /// The client is connected and has begun joining a
        /// schedule.
        /// <see cref="UserViewModel.SharedScheduleVM"/> is available.
        /// <see cref="UserViewModel.TailoredScheduleVM"/>,
        /// <see cref="UserViewModel.SearchVM"/> and
        /// <see cref="UserViewModel.Name"/> are not yet set.
        /// </summary>
        JoiningSchedule,

        /// <summary>
        /// The client has joined a schedule and is registered as
        /// a user in the schedule.
        /// <see cref="UserViewModel.SharedScheduleVM"/>,
        /// <see cref="UserViewModel.TailoredScheduleVM"/>,
        /// <see cref="UserViewModel.SearchVM"/> and
        /// <see cref="UserViewModel.Name"/> are available.
        /// </summary>
        JoinedSchedule
    }
}
