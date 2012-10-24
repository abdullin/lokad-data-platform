using System;
using System.Web.Mvc;
using Platform;
using Platform.ViewClients;
using SmartApp.Sample3.Contracts;

namespace SmartApp.Sample3.WebUI.Controllers
{
    public class HomeController : Controller
    {
        // TODO: put into config
        const string config = @"C:\LokadData\dp-store";

        static readonly ViewClient Global = PlatformClient.GetViewClient(config, Conventions.ViewContainer);
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Tags()
        {
            var model = Global.ReadAsJsonOrGetNew<TagsDistributionView>(TagsDistributionView.FileName);
            var info = Global.ReadAsJsonOrGetNew<ProcessingInfoView>(TagsDistributionView.FileName + ".info");
            return PartialView(Tuple.Create(model, info));
        }

        public ActionResult Comments()
        {
            var model = Global.ReadAsJsonOrGetNew<CommentDistributionView>(CommentDistributionView.FileName);
            var info = Global.ReadAsJsonOrGetNew<ProcessingInfoView>(CommentDistributionView.FileName + ".info");
            return PartialView(Tuple.Create(model, info));
        }

        public ActionResult Users()
        {
            var model = Global.ReadAsJsonOrGetNew<UserCommentsDistributionView>(UserCommentsDistributionView.FileName);
            var info = Global.ReadAsJsonOrGetNew<ProcessingInfoView>(UserCommentsDistributionView.FileName + ".info");
            return PartialView(Tuple.Create(model, info));
        }
    }
}
