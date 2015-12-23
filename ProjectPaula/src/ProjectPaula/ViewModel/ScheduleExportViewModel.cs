using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using ProjectPaula.Model;
using ProjectPaula.Model.CalendarExport;
using ProjectPaula.Model.ObjectSynchronization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPaula.ViewModel
{
    public class ScheduleExportViewModel : BindableBase
    {
        private Schedule _schedule;

        private string _exportUrl;

        public string ExportUrl
        {
            get { return _exportUrl; }
            set { Set(ref _exportUrl, value); }
        }

        public ScheduleExportViewModel(Schedule schedule)
        {
            _schedule = schedule;

        }

        public void ExportSchedule(User user)
        {

            var request = HttpHelper.HttpContext.Request;

            // Workaround for Outlook Online (avoid https and redirection to https from main URL)
            var host = request.Scheme.Equals("https") ? "webcal." + request.Host.Value : request.Host.Value;
            ExportUrl = $"http://{host}/paul/ExportSchedule?id={_schedule.Id}&username={user.Name.ToBase64String()}";
        }
    }
}
