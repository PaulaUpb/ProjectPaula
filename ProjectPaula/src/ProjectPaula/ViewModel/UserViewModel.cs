using Newtonsoft.Json;
using ProjectPaula.Model;
using ProjectPaula.Model.ObjectSynchronization;
using ProjectPaula.Model.ObjectSynchronization.ChangeTracking;
using ProjectPaula.Util;
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

        /// <summary>
        /// The database user object.
        /// This is only available if the user has joined
        /// a schedule, i.e. if <see cref="State"/> is
        /// <see cref="SessionState.JoinedSchedule"/>.
        /// Otherwise, this is null.
        /// </summary>
        public User User => SharedScheduleVM?.Schedule.Users.FirstOrDefault(user => user.Name == Name);

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

        [JsonProperty]
        public UserErrorsViewModel Errors { get; } = new UserErrorsViewModel();

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

        /// <summary>
        /// The export view model that is shared across all users
        /// </summary>
        public ScheduleExportViewModel ExportVM { get; private set; }

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
                    await _scheduleManager.SaveScheduleAsync(SharedScheduleVM);
                    SharedScheduleVM = null;
                    TailoredScheduleVM = null;
                    SearchVM = null;
                    ExportVM = null;
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
        public bool BeginJoinSchedule(string scheduleID, ErrorReporter errorReporter)
        {
            if (State != SessionState.Default)
            {

                errorReporter.SetMessage(UserErrorsViewModel.WrongSessionStateMessage);
                return false;
            }

            var scheduleVM = _scheduleManager.GetOrLoadSchedule(scheduleID);

            if (scheduleVM == null)
            {
                errorReporter.SetMessage(UserErrorsViewModel.ScheduleIdInvalidMessage);
                return false;
            }

            SharedScheduleVM = scheduleVM;
            State = SessionState.JoiningSchedule;
            return true;
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
        /// <param name="errorReporter">Object used to throw exceptions</param>
        /// <returns>True if the join completion was successful</returns>
        public async Task CompleteJoinScheduleAsync(string userName, ErrorReporter errorReporter)
        {
            if (State != SessionState.JoiningSchedule)
            {
                errorReporter.Throw(UserErrorsViewModel.GenericErrorMessage + " (Falscher SessionState)");
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                errorReporter.Throw("Es wurde ein ungültiger Name eingegeben");
            }

            Name = userName;

            // This fails if user name is invalid (empty) or already used by another client
            await SharedScheduleVM.AddUserAsync(this, errorReporter);

            TailoredScheduleVM = ScheduleViewModel.CreateFrom(SharedScheduleVM.Schedule, errorReporter);
            SearchVM = new CourseSearchViewModel(SharedScheduleVM.Schedule.CourseCatalogue, SharedScheduleVM.Schedule);
            ExportVM = new ScheduleExportViewModel(SharedScheduleVM.Schedule);

            State = SessionState.JoinedSchedule;
        }

        /// <summary>
        /// Creates a new schedule with a random identifier
        /// and makes the client join it using the specified user name.
        /// </summary>
        public async Task CreateAndJoinScheduleAsync(string userName, string catalogId, ErrorReporter errorReporter)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                errorReporter.Throw(
                    new ArgumentException("The name of the specified user is invalid", nameof(userName)),
                    UserErrorsViewModel.UserNameInvalidMessage);
            }

            if (State != SessionState.Default)
            {
                errorReporter.Throw(
                    new InvalidOperationException(),
                    UserErrorsViewModel.WrongSessionStateMessage);
            }

            SharedScheduleVM = await _scheduleManager.CreateScheduleAsync(catalogId, errorReporter);
            TailoredScheduleVM = ScheduleViewModel.CreateFrom(SharedScheduleVM.Schedule, errorReporter);
            SearchVM = new CourseSearchViewModel(SharedScheduleVM.Schedule.CourseCatalogue, SharedScheduleVM.Schedule);
            ExportVM = new ScheduleExportViewModel(SharedScheduleVM.Schedule);

            // Add user to list of current users
            Name = userName;
            await SharedScheduleVM.AddUserAsync(this, errorReporter);

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
