using Microsoft.AspNetCore.Http;

namespace ProjectPaula.Model.CalendarExport
{
    public static class HttpHelper
    {
        private static HttpContext _httpContext;


        public static HttpContext HttpContext
        {
            get { return _httpContext; }
            set { if (_httpContext == null) _httpContext = value; }
        }
    }

    public class QueryValueService
    {
        private readonly IHttpContextAccessor _accessor;

        public QueryValueService(IHttpContextAccessor httpContextAccessor)
        {
            _accessor = httpContextAccessor;
        }

        public string GetValue()
        {
            return _accessor.HttpContext.Request.Query["value"];
        }
    }
}
