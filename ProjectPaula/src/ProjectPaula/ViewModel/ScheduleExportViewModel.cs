
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using ProjectPaula.Model;
using ProjectPaula.Model.CalendarExport;
using ProjectPaula.Model.ObjectSynchronization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public async Task ExportSchedule()
        {
            var hosting = CallContextServiceLocator.Locator.ServiceProvider
                            .GetRequiredService<IApplicationEnvironment>();
            
            var basePath = hosting.ApplicationBasePath;

            var filePath = basePath + $"/Calendars/schedule{_schedule.Id}.ics";
            if (!Directory.Exists(basePath + "/Calendars")) { Directory.CreateDirectory(basePath + "/Calendars"); }
            var calendar = ScheduleExporter.ExportSchedule(_schedule);
            if (!File.Exists(filePath))
            {
                var file = File.Open(filePath, FileMode.OpenOrCreate);
                using (var writer = new StreamWriter(file))
                {
                    await writer.WriteAsync(calendar);
                    await writer.FlushAsync();
                }
                file.Dispose();
            }
            var request = HttpHelper.HttpContext.Request;
            
            ExportUrl = $"{request.Scheme}://{request.Host.Value}/paul/ExportSchedule?id={_schedule.Id}";
            

        }
    }
}
