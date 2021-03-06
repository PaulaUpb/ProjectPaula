﻿using ProjectPaula.DAL;
using ProjectPaula.Model;
using ProjectPaula.Model.CalendarExport;
using ProjectPaula.Model.ObjectSynchronization;

namespace ProjectPaula.ViewModel
{
    public class ScheduleExportViewModel : BindableBase
    {
        private readonly Schedule _schedule;

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
            var request = UrlHelper.HttpContext.Request;
            // Workaround for Outlook Online (avoid https and redirection to https from main URL) 
            var host = PaulRepository.IsHttps ? "webcal." + request.Host.Value : request.Host.Value;
            ExportUrl = $"http://{host}/ExportSchedule?id={_schedule.Id}&username={user.Name.ToBase64String()}";

        }
    }
}
