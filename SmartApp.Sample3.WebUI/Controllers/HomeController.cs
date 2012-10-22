using System.Web.Mvc;
using Platform;
using SmartApp.Sample3.Contracts;

namespace SmartApp.Sample3.WebUI.Controllers
{


    public class HomeController : Controller
    {
        // TODO: put into config
        const string config = @"C:\LokadData\dp-store";

        static readonly ViewClient Global = PlatformClient
            .GetViewClient(config, Conventions.ViewContainer);
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Tags()
        {
            var model = Global .ReadAsJsonOrNull<TagsDistributionView>(TagsDistributionView.FileName) ??
                new TagsDistributionView();
            return PartialView( model);
        }

        public ActionResult Comments()
        {
            var model = Global.ReadAsJsonOrNull<CommentDistributionView>(CommentDistributionView.FileName) ??
                new CommentDistributionView();
            return PartialView(model);
        }
    }
}
