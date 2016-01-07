using Microsoft.AspNet.Http;

namespace ProjectPaula.Model.CalendarExport
{
    public static class HttpHelper
    {
        private static IHttpContextAccessor _httpContextAccessor;

        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public static HttpContext HttpContext => _httpContextAccessor.HttpContext;
    }
}
