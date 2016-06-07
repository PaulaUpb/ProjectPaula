using Microsoft.AspNetCore.Http;

namespace ProjectPaula.Model.CalendarExport
{
    public static class UrlHelper
    {
        public static string Scheme { get; private set; }

        public static string Host { get; private set; }

        public static void Initialize(HttpContext context)
        {
            if (string.IsNullOrEmpty(Scheme)) Scheme = context.Request.Scheme;
            if (string.IsNullOrEmpty(Host)) Host = context.Request.Host.Value;
        }
    }

}
