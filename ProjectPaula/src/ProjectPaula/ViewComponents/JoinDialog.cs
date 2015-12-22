using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace ProjectPaula.ViewComponents
{
    public class JoinDialog : ViewComponent
    {
        public IViewComponentResult Invoke() => View();

    }
}
