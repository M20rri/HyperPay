using HyperPay.Shared.Models;
using System.Data.Entity;
using System.Web;
using System.Web.Http;

namespace HyperPay.Mobile
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            Database.SetInitializer<HyperPayContext>(null);
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
