using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Mvc;
using ServiceStack.Text;
using SmartApp.Sample3.Contracts;

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

        Sample3Data GetProjectionViewData()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\SmartApp.Sample3.Continuous\bin\Debug\sample3-tag-count.dat");

            if (!System.IO.File.Exists(path))
                return null;

            return System.IO.File.ReadAllText(path).FromJson<Sample3Data>();
        }

    }
}
