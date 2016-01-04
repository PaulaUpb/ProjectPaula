using Microsoft.Data.Entity;
using Newtonsoft.Json;
using ProjectPaula.DAL;
using ProjectPaula.Model;
using ProjectPaula.Model.ObjectSynchronization;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        public string Id
        {
            get { return Schedule.Id; }
        }

        [JsonProperty]
        public string Name { get { return Schedule.Name; } }

        /// <summary>
        /// Gets a list of users that are connected and have joined
        /// the schedule specified with <see cref="Schedule"/>.
        /// </summary>
        [JsonProperty]
        public ObservableCollectionEx<UserViewModel> Users { get; } = new ObservableCollectionEx<UserViewModel>();

        /// <summary>
        /// Gets a list of usernames that are known but not currently
        /// used by any client.
        /// </summary>
        [JsonProperty]
        public ObservableCollectionEx<string> AvailableUserNames { get; }

        /// <summary>
        /// Semaphore only for use in the TimetableHub.
        /// </summary>
        public SemaphoreSlim TimetableHubSemaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        /// <summary>
        /// Initializes a new <see cref="SharedScheduleViewModel"/>
        /// with the specified <see cref="Schedule"/>.
        /// All known user names are initially added to <see cref="AvailableUserNames"/>
        /// and the <see cref="Users"/> collection is empty.
        /// </summary>
        /// <param name="schedule">Schedule</param>
        /// <param name="id">Schedule ID (this is temporary, should later be determined by the schedule object itself)</param>
        public SharedScheduleViewModel(Schedule schedule)
        {
            Schedule = schedule;
            AvailableUserNames = new ObservableCollectionEx<string>(
                schedule.Users.Select(user => user.Name));
        }

        /// <summary>
        /// Adds the specified user to the list of current users.
        /// The user name can either be a known user name (i.e. a user
        /// that has already joined the schedule sometime before) or
        /// a new user name (in this case the user's info is added to the DB).
        /// </summary>
        /// <param name="userVM"></param>
        public async Task AddUserAsync(UserViewModel userVM)
        {
            if (userVM == null)
                throw new ArgumentNullException(nameof(userVM));

            if (string.IsNullOrWhiteSpace(userVM.Name))
                throw new ArgumentException("The name of the specified user is invalid", nameof(userVM));

            if (Users.Any(o => o.Name == userVM.Name))
                throw new ArgumentException($"The user name '{userVM.Name}' is already in use", nameof(userVM.Name));

            if (Schedule.Users.Any(o => o.Name == userVM.Name))
            {
                // This is a known user name
                Users.Add(userVM);
                AvailableUserNames.Remove(userVM.Name);
            }
            else
            {
                // The client is a new user
                Users.Add(userVM);

                // Create new known user in DB
                var dbUser = new User { Name = userVM.Name };
                await PaulRepository.AddUserToScheduleAsync(Schedule, dbUser);
            }
        }

        /// <summary>
        /// Removes the specified user from the list of current users.
        /// The user's name and selected courses remain in the database.
        /// </summary>
        /// <param name="userVM"></param>
        public void RemoveUser(UserViewModel userVM)
        {
            if (userVM == null)
                throw new ArgumentNullException(nameof(userVM));

            if (Users.Remove(userVM))
            {
                // The user has left, so if a new user joins the schedule 
                // we can suggest that name again
                AvailableUserNames.Add(userVM.Name);
            }
        }

        public async Task ChangeScheduleName(string name)
        {
            await PaulRepository.ChangeScheduleName(Schedule, name);
            RaisePropertyChanged(nameof(Name));
        }
    }
}
