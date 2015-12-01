using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ProjectPaula.Model.ObjectSynchronization;
using ProjectPaula.Model.ObjectSynchronization.ChangeTracking;
using System;
using System.Collections.Generic;
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
                    SharedScheduleVM.RemoveUser(this);
                    //removed temporarily to avoid tracking exception
                    //await _scheduleManager.SaveScheduleAsync(SharedScheduleVM);
                    SharedScheduleVM = null;
                    TailoredScheduleVM = null;
                    SearchVM = null;
                    Name = null;
                    break;

                default:
                    throw new NotImplementedException();
            }

            State = SessionState.Default;
            await Task.FromResult(0);
        }

        /// <summary>
        /// Loads the schedule with the specified ID and assigns it
        /// to the calling client. After this the client is expected
        /// to choose a user name and then call
        /// <see cref="CompleteJoinScheduleAsync(string)"/> to actually join
        /// the schedule and start collaborating with others.
        /// </summary>
        /// <remarks>
        /// After this call, synchronization of <see cref="SharedScheduleVM"/>
        /// with the calling client should start.
        /// </remarks>
        /// <param name="scheduleID">Schedule ID</param>
        public void BeginJoinSchedule(string scheduleID)
        {
            if (State != SessionState.Default)
                throw new InvalidOperationException("The client has already joined a schedule");

            var scheduleVM = _scheduleManager.GetOrLoadSchedule(scheduleID);

            if (scheduleVM == null)
                throw new ArgumentException($"There is no schedule with ID '{scheduleID}'");

            SharedScheduleVM = scheduleVM;
            State = SessionState.JoiningSchedule;
        }

        /// <summary>
        /// Joins the schedule specified by <see cref="BeginJoinSchedule(string)"/>
        /// by adding the calling client to the schedule's list of users.
        /// </summary>
        /// <remarks>
        /// After this call, synchronization of <see cref="TailoredScheduleVM"/>
        /// and <see cref="SearchVM"/> with the calling client should start.
        /// </remarks>
        /// <param name="userName">
        /// User name (either one of the schedule's known but currently unused user names
        /// (see <see cref="SharedScheduleViewModel.AvailableUserNames"/> or a new name).
        /// </param>
        public async Task CompleteJoinScheduleAsync(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentException(nameof(userName));

            if (State != SessionState.JoiningSchedule)
                throw new InvalidOperationException();

            // This fails if user name is invalid (empty) or already used by another client
            Name = userName;
            await SharedScheduleVM.AddUserAsync(this);

            TailoredScheduleVM = ScheduleViewModel.CreateFrom(SharedScheduleVM.Schedule);
            SearchVM = new CourseSearchViewModel(SharedScheduleVM.Schedule.CourseCatalogue);

            State = SessionState.JoinedSchedule;
        }

        /// <summary>
        /// Creates a new schedule with a random identifier
        /// and makes the client join it using the specified user name.
        /// </summary>
        public async Task CreateAndJoinScheduleAsync(string userName, string catalogId)
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new ArgumentException(nameof(userName));

            if (State != SessionState.Default)
                throw new InvalidOperationException();

            SharedScheduleVM = await _scheduleManager.CreateScheduleAsync(catalogId);
            TailoredScheduleVM = ScheduleViewModel.CreateFrom(SharedScheduleVM.Schedule);
            SearchVM = new CourseSearchViewModel(SharedScheduleVM.Schedule.CourseCatalogue);

            // Add user to list of current users
            Name = userName;
            await SharedScheduleVM.AddUserAsync(this);

            State = SessionState.JoinedSchedule;
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
