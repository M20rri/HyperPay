using HyperPay.Shared.Dtos;
using HyperPay.Shared.Models;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace HyperPay.Mobile.Helpers
{
    public class CredentialValidation
    {
        public DTOUserMaster CheckCredential(string username, string password)
        {
            using (var ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_CheckLoginCreds '{username}','{password}'";
                return ctx.Database.SqlQuery<DTOUserMaster>(query).FirstOrDefault();
            }
        }

        public bool CheckRoutePrefixMapping(string prefix, int userId)
        {
            bool isMapped = false;
            using (var ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_CheckUserRoute '{prefix}',{userId}";
                var userRoute = ctx.Database.SqlQuery<DTOUserMaster>(query).FirstOrDefault();
                if (userRoute != null)
                {
                    isMapped = true;
                }
            }

            return isMapped;
        }

    }

    public class RouteAuthenticateHandler : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            bool isAuthorized = false;

            try
            {
                var headerToken = actionContext.Request.Headers.GetValues("Authorization").FirstOrDefault();

                if (headerToken != null)
                {
                    string[] tokensValues = headerToken.Split(':');

                    DTOUserMaster CurrentUser = new CredentialValidation().CheckCredential(tokensValues[0], tokensValues[1]);

                    if (CurrentUser != null)
                    {
                        var routePrefix = actionContext.ActionDescriptor.ActionName;
                        var isMapped = new CredentialValidation().CheckRoutePrefixMapping(routePrefix, CurrentUser.Id);

                        if (isMapped)
                        {
                            IPrincipal principal = new GenericPrincipal(new GenericIdentity(JsonConvert.SerializeObject(CurrentUser)), null);
                            Thread.CurrentPrincipal = principal;
                            HttpContext.Current.User = principal;
                            isAuthorized = true;
                        }

                    }
                    else
                    {
                        return false;
                    }
                }

            }
            catch (Exception ex)
            {
                isAuthorized = false;
            }


            return isAuthorized;
        }
    }
}