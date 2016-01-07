using ProjectPaula.Model.ObjectSynchronization;

namespace ProjectPaula.ViewModel
{
    public class UserErrorsViewModel : BindableBase
    {
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

        /// <summary>
        /// The error message that is displayed in the
        /// schedule creation dialog.
        /// </summary>
        public string ScheduleCreationMessage
        {
            get { return _scheduleCreationMessage; }
            set { Set(ref _scheduleCreationMessage, value); }
        }

        /// <summary>
        /// The error message that is displayed in the join dialog
        /// during name selection.
        /// </summary>
        public string ScheduleJoinMessage
        {
            get { return _scheduleJoinMessage; }
            set { Set(ref _scheduleJoinMessage, value); }
        }

        /// <summary>
        /// The error message that is displayed at the top of the schedule view.
        /// </summary>
        public string ScheduleMessage
        {
            get { return _scheduleMessage; }
            set { Set(ref _scheduleMessage, value); }
        }

        /// <summary>
        /// The error message that is displayed in the course search dialog.
        /// </summary>
        public string CourseSearchMessage
        {
            get { return _courseSearchMessage; }
            set { Set(ref _courseSearchMessage, value); }
        }
    }
}
