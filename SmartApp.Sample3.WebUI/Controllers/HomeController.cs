using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Mvc;
using Platform;
using ServiceStack.Text;
using SmartApp.Sample3.Contracts;

namespace SmartApp.Sample3.WebUI.Controllers
{


    public class HomeController : Controller
    {
        // TODO: put into config
        const string config = @"C:\LokadData\dp-store";

        static readonly IViewContainer Global = PlatformClient
            .ViewClient(config)
            .GetContainer(Conventions.ViewContainer);
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Tags()
        {
            var model = GetTagProjectionViewData();
            return PartialView( model);
        }

        static TagsDistributionView GetTagProjectionViewData()
        {
            if (!Global.Exists(TagsDistributionView.FileName))
                return null;

            using (var stream = Global.OpenRead(TagsDistributionView.FileName))
            {
                return JsonSerializer.DeserializeFromStream<TagsDistributionView>(stream);
            }
        }

        public ActionResult Comments()
        {
            var model = GetCommentProjectionViewData();
            return PartialView(model);
        }

        CommentDistributionView GetCommentProjectionViewData()
        {
            if (!Global.Exists(CommentDistributionView.FileName))
                return null;

            using (var stream = Global.OpenRead(CommentDistributionView.FileName))
            {
                return JsonSerializer.DeserializeFromStream<CommentDistributionView>(stream);
            }
        }

    }
}
