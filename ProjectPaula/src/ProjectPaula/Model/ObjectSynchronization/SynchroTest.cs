using ProjectPaula.Model.ObjectSynchronization.ChangeTracking;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ProjectPaula.Model.ObjectSynchronization
{
    public class SynchroTest
    {
        public static void Test()
        {
            var vm = new TestViewModel
            {
                FirstName = "Sven",
                Birthdays = new ObservableCollection<DateViewModel>()
                {
                    new DateViewModel { Month = 10, Year = 1994 },
                    new DateViewModel { Month = 5, Year = 2015 },
                }
            };

            var tracker = new ObjectTracker(vm);
            tracker.PropertyChanged += (sender, e) => Debug.WriteLine($"'{e.PropertyPath}' changed from '{e.OldValue}' to '{e.NewValue}'");
            tracker.CollectionChanged += (sender, e) => Debug.WriteLine($"'{e.PropertyPath}' collection changed: {e.Action}, Items={e.Items?.Count ?? 0}, StartingIndex={e.StartingIndex}");
        }

        class TestViewModel : BindableBase
        {
            private string _firstName;
            private ObservableCollection<DateViewModel> _birthdays;

            public string FirstName
            {
                get { return _firstName; }
                set { Set(ref _firstName, value); }
            }

            public ObservableCollection<DateViewModel> Birthdays
            {
                get { return _birthdays; }
                set { Set(ref _birthdays, value); }
            }
        }

        class DateViewModel : BindableBase
        {
            private int _month;
            private int _year;
            private ObservableCollection<string> _names = new ObservableCollection<string>();

            public int Month
            {
                get { return _month; }
                set { Set(ref _month, value); }
            }

            public int Year
            {
                get { return _year; }
                set { Set(ref _year, value); }
            }

            public ObservableCollection<string> Names => _names;
        }
    }
}
