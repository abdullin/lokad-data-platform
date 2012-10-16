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
            return View();
        }

        public ActionResult Tags()
        {
            var model = GetTagProjectionViewData();
            return PartialView( model);
        }

        TagsDistributionView GetTagProjectionViewData()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\SmartApp.Sample3.Continuous\bin\Debug\sample3-tag-count.dat");

            if (!System.IO.File.Exists(path))
                return null;

            return System.IO.File.ReadAllText(path).FromJson<TagsDistributionView>();
        }

        public ActionResult Comments()
        {
            var model = GetCommentProjectionViewData();
            return PartialView(model);
        }

        CommentDistributionView GetCommentProjectionViewData()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\SmartApp.Sample3.Continuous\bin\Debug\sample3-comment.dat");

            if (!System.IO.File.Exists(path))
                return null;

            return System.IO.File.ReadAllText(path).FromJson<CommentDistributionView>();
        }

    }
}
