using System.Web.Mvc;
using Platform;
using Platform.ViewClient;
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
            return PartialView( model);
        }

        public ActionResult Comments()
        {
            var model = Global.ReadAsJsonOrGetNew<CommentDistributionView>(CommentDistributionView.FileName);
            return PartialView(model);
        }
    }
}
