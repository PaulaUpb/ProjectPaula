using System;
using System.Linq;
using System.Threading.Tasks;
using ProjectPaula.DAL;
using ProjectPaula.Model;
using ProjectPaula.Model.ObjectSynchronization;
using ProjectPaula.ViewModel;

namespace ProjectPaula.Hubs
{
    public class TimetableHub : ObjectSynchronizationHub<IObjectSynchronizationHubClient>
    {
        private UserViewModel CallingClient => ScheduleManager.Instance.Clients[Context.ConnectionId];

        public override async Task OnConnected()
        {
            await base.OnConnected();
            await ScheduleManager.Instance.AddClientAsync(Context.ConnectionId);

            // Begin synchronization of public VM and User VM
            CallerSynchronizedObjects["Public"] = await ScheduleManager.Instance.GetPublicViewModelAsync();
            CallerSynchronizedObjects["User"] = CallingClient;
        }

        public override async Task OnDisconnected(bool stopCalled)
        {
            await ScheduleManager.Instance.RemoveClientAsync(Context.ConnectionId);
            await base.OnDisconnected(stopCalled);
        }

        public void BeginJoinSchedule(string scheduleID)
        {
            // This loads the SharedScheduleVM and assigns it to the client
            CallingClient.BeginJoinSchedule(scheduleID);

            // Begin synchronization of shared schedule VM
            CallerSynchronizedObjects["SharedSchedule"] = CallingClient.SharedScheduleVM;
        }

        public async Task CompleteJoinSchedule(string userName)
        {
            // This adds the client to the list of users and creates
            // a tailored schedule VM and a search VM
            await CallingClient.CompleteJoinScheduleAsync(userName);

            // Begin synchronization of tailored schedule VM and search VM
            CallerSynchronizedObjects["TailoredSchedule"] = CallingClient.TailoredScheduleVM;
            CallerSynchronizedObjects["Search"] = CallingClient.SearchVM;
            CallerSynchronizedObjects["Export"] = CallingClient.ExportVM;

        }

        public async Task CreateSchedule(string userName, string catalogId)
        {
            // Create a new schedule and make the user join it
            await CallingClient.CreateAndJoinScheduleAsync(userName, catalogId);

            // Begin synchronization of VMs
            CallerSynchronizedObjects["SharedSchedule"] = CallingClient.SharedScheduleVM;
            CallerSynchronizedObjects["TailoredSchedule"] = CallingClient.TailoredScheduleVM;
            CallerSynchronizedObjects["Search"] = CallingClient.SearchVM;
            CallerSynchronizedObjects["Export"] = CallingClient.ExportVM;
        }

        public async Task ExitSchedule()
        {
            await CallingClient.DisconnectAsync();

            // Remove VMs from client
            CallerSynchronizedObjects["SharedSchedule"] = null;
            CallerSynchronizedObjects["TailoredSchedule"] = null;
            CallerSynchronizedObjects["Search"] = null;
            CallerSynchronizedObjects["Export"] = null;
        }

        public void ExportSchedule()
        {
            if (CallingClient.ExportVM != null)
            {
                CallingClient.ExportVM.ExportSchedule(this.CallingClient.User);
            }
        }

        /// <summary>
        /// RPC-method for updating the searchQuery
        /// </summary>
        /// <param name="searchQuery"></param>
        public void SearchCourses(string searchQuery)
        {
            if (CallingClient.SearchVM != null)
            {
                CallingClient.SearchVM.SearchQuery = searchQuery;
            }
        }

        /// <summary>
        /// RPC-method for adding a course to the schedule and for
        /// adding the calling user to a course that someone else
        /// has already added.
        /// </summary>
        /// <param name="courseId">Course ID</param>
        /// <returns></returns>
        public async Task AddCourse(string courseId)
        {
            var course = PaulRepository.Courses.FirstOrDefault(c => c.Id == courseId);
            if (course == null)
            {
                throw new ArgumentException("Course not found", nameof(courseId));
            }

            var schedule = CallingClient.SharedScheduleVM.Schedule;
            var selectedCourse = schedule.SelectedCourses.FirstOrDefault(c => c.CourseId == courseId);

            if (selectedCourse == null)
            {
                if (course.IsTutorial)
                {
                    // The user has decided to select a pending tutorial
                    CallingClient.TailoredScheduleVM.RemovePendingTutorials(course);
                }

                await PaulRepository.AddCourseToScheduleAsync(schedule, courseId, CallingClient.User);
                AddTutorialsToTailoredViewModel(courseId, CallingClient);
            }
            else if (selectedCourse.Users.All(u => u.User != CallingClient.User))
            {
                // The course has already been added to the schedule by someone else.
                // Add the calling user to the selected course (if not yet done).
                await PaulRepository.AddUserToSelectedCourseAsync(selectedCourse, CallingClient.User);
            }

            UpdateTailoredViewModels();
        }

        private void AddTutorialsToTailoredViewModel(string courseId, UserViewModel user)
        {
            var course = PaulRepository.Courses.Find(c => c.Id == courseId);
            var tutorials = course.Tutorials
                .Concat(
                    course.ConnectedCourses
                        .Where(connectedCourse => !connectedCourse.IsTutorial)
                        .SelectMany(connectedCourse => connectedCourse.Tutorials)
                )
                .ToList();
            user.TailoredScheduleVM.AddPendingTutorials(tutorials);
            user.TailoredScheduleVM.UpdateFrom(CallingClient.SharedScheduleVM.Schedule);
        }

        /// <summary>
        /// RPC-method for removing a course from the schedule
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns></returns>
        public async Task RemoveCourse(string courseId)
        {
            var course = PaulRepository.Courses.FirstOrDefault(c => c.Id == courseId);
            if (course == null)
            {
                throw new ArgumentException("Course not found", nameof(courseId));
            }

            var schedule = CallingClient.SharedScheduleVM.Schedule;

            if (schedule.SelectedCourses.Any(c => c.CourseId == courseId))
            {
                await PaulRepository.RemoveCourseFromScheduleAsync(schedule, courseId);
                UpdateTailoredViewModels();
            }
            else if (course.IsTutorial)
            {
                // The user has decided to remove a tutorial before joining one
                CallingClient.TailoredScheduleVM.RemovePendingTutorials(course);
                UpdateTailoredViewModels();
            }
            else
            {
                throw new ArgumentException("Course not found in the schedule!");
            }
        }

        /// <summary>
        /// Removes the calling user from the specified selected course.
        /// If after the removal no other user has selected the course,
        /// the course is removed from the schedule.
        /// </summary>
        /// <remarks>
        /// If the user has not selected the course with the specified ID,
        /// nothing happens.
        /// </remarks>
        /// <param name="courseId">Course ID</param>
        /// <returns></returns>
        public async Task RemoveUserFromCourse(string courseId)
        {
            if (PaulRepository.Courses.All(c => c.Id != courseId))
            {
                throw new ArgumentException("Course not found", nameof(courseId));
            }

            var schedule = CallingClient.SharedScheduleVM.Schedule;

            var selectedCourse = schedule.SelectedCourses
                .FirstOrDefault(c => c.CourseId == courseId);

            if (selectedCourse == null)
            {
                throw new ArgumentException("Course not found in the schedule!");
            }
            else
            {
                var selectedCourseUser = selectedCourse.Users.FirstOrDefault(o => o.User == CallingClient.User);

                if (selectedCourseUser != null)
                {
                    // Remove user from selected course
                    await PaulRepository.RemoveUserFromSelectedCourseAsync(selectedCourse, selectedCourseUser);
                }

                if (!selectedCourse.Users.Any())
                {
                    // The course is no longer selected by anyone
                    // -> Remove the whole course from schedule
                    await PaulRepository.RemoveCourseFromScheduleAsync(schedule, courseId);
                }

                UpdateTailoredViewModels();
            }
        }

        /// <summary>
        /// Updates the tailored VMs of all users that have joined
        /// the same schedule as the calling user in order to
        /// reflect changes made to the model objects.
        /// </summary>
        /// <remarks>
        /// This approach is quite inefficient. In the future we should
        /// find an easier way to update schedules on all clients at once.
        /// Probably the number of shared properties should be increased
        /// while decreasing the number of tailored properties.
        /// Furthermore, some of the tailored properties could probably
        /// be moved to the client side.
        /// </remarks>
        private void UpdateTailoredViewModels()
        {
            foreach (var user in CallingClient.SharedScheduleVM.Users)
            {
                user.TailoredScheduleVM.UpdateFrom(CallingClient.SharedScheduleVM.Schedule);
            }
        }
    }
}
