using ProjectPaula.Model.CalendarExport;

namespace ProjectPaula.Util
{
    public class UrlGeneratorUtil
    {
        /// <summary>
        /// Generate a URL of the form {scheme}://{host}/?ScheduleId={id}
        /// </summary>
        public static string GenerateScheduleUrl(string scheduleId)
        {
            var request = UrlHelper.HttpContext.Request;
            var scheme = request.Scheme;
            var host = request.Host;
            return $"{scheme}://{host}/?ScheduleId={scheduleId}";

        }

        public static string GenerateFacebookMessageUrl(string shareUrl) => $"http://www.facebook.com/dialog/send?app_id=962979217094807&link={shareUrl}&redirect_uri=https://facebook.com";
    }
}