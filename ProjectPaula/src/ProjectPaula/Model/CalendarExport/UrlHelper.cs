using System;
using Microsoft.AspNetCore.Http;

namespace ProjectPaula.Model.CalendarExport
{
    public static class UrlHelper
    {

        private static IHttpContextAccessor _httpContextAccessor;

        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            if (_httpContextAccessor == null)
                _httpContextAccessor = httpContextAccessor;
        }

        public static HttpContext HttpContext
        {
            get
            {
                return _httpContextAccessor.HttpContext;
            }
        }

    }


    public class HttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext HttpContext { get; set; }
    }

}
