using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Mvc;
using ServiceStack.Text;
using SmartApp.Sample3.Continuous;

namespace SmartApp.Sample3.WebUI.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var model = GetProjectionViewData();
            if (Request.IsAjaxRequest())
            {
                return PartialView("ProjectionView", model);
            }

            return View(model);
        }

        Dictionary<long, long> GetProjectionViewData()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\SmartApp.Sample3.Continuous\bin\Debug\sample3.dat");

            if (!System.IO.File.Exists(path))
                return null;

            return System.IO.File.ReadAllText(path).FromJson<Sample3Data>().Distribution;
        }

    }
}
