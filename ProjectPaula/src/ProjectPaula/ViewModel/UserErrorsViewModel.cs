using ProjectPaula.Model.ObjectSynchronization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectPaula.ViewModel
{
    public class UserErrorsViewModel : BindableBase
    {
        private static readonly int _timeUntilMessagesAreCleared = 5000; // milliseconds

        public const string GenericErrorMessage = "Da hat etwas nicht funktioniert, bitte aktualisiere die Seite und versuche es noch einmal. Wenn das Problem weiterhin auftritt, kontaktiere uns.";
        public const string WrongSessionStateMessage = GenericErrorMessage + " (Falscher Sitzungszustand)";
        public const string UserNameInvalidMessage = "Der angegebene Name ist ungültig";
        public const string UserNameAlreadyInUseMessage = "Der angegebene Name wird bereits verwendet";
        public const string ScheduleIdInvalidMessage = "Dieser Stundenplan existiert nicht (mehr)";
        public const string SearchQueryTooShortMessage = "Die Suchanfrage muss mindestens drei Zeichen lang sein";

        private string _scheduleCreationMessage;
        private string _scheduleJoinMessage;
        private string _scheduleMessage;
        private string _courseSearchMessage;
        private Timer _timer;

        public UserErrorsViewModel()
        {
            _timer = new Timer(_ => ClearMessages(), null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// The error message that is displayed in the
        /// schedule creation dialog.
        /// </summary>
        public string ScheduleCreationMessage
        {
            get { return _scheduleCreationMessage; }
            set
            {
                Set(ref _scheduleCreationMessage, value);
                ClearMessagesAfterTime();
            }
        }

        /// <summary>
        /// The error message that is displayed in the join dialog
        /// during name selection.
        /// </summary>
        public string ScheduleJoinMessage
        {
            get { return _scheduleJoinMessage; }
            set
            {
                Set(ref _scheduleJoinMessage, value);
                ClearMessagesAfterTime();
            }
        }

        /// <summary>
        /// The error message that is displayed at the top of the schedule view.
        /// </summary>
        public string ScheduleMessage
        {
            get { return _scheduleMessage; }
            set
            {
                Set(ref _scheduleMessage, value);
                ClearMessagesAfterTime();
            }
        }

        /// <summary>
        /// The error message that is displayed in the course search dialog.
        /// </summary>
        public string CourseSearchMessage
        {
            get { return _courseSearchMessage; }
            set
            {
                Set(ref _courseSearchMessage, value);
                ClearMessagesAfterTime();
            }
        }

        private void ClearMessagesAfterTime()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite); // Stop timer
            _timer.Change(_timeUntilMessagesAreCleared, Timeout.Infinite); // Restart timer
        }

        private void ClearMessages()
        {
            ScheduleCreationMessage = null;
            ScheduleJoinMessage = null;
            ScheduleMessage = null;
            CourseSearchMessage = null;
        }
    }
}
