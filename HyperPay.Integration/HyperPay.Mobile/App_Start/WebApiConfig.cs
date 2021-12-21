using HyperPay.Mobile.Helpers;
using System.Web.Http;
using System.Web.Http.Cors;

namespace HyperPay.Mobile
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            config.Filters.Add(new RouteAuthenticateHandler());

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                            "DefaultApi",
                            "api/{action}/{id}",
                            new { id = RouteParameter.Optional }
                        );

            config.MessageHandlers.Add(new LogRequestAndResponseHandler());

            GlobalConfiguration.Configuration.Formatters.XmlFormatter.SupportedMediaTypes.Clear();

            EnableCorsAttribute cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);

        }
    }
}
