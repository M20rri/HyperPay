using HyperPay.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace HyperPay.Mobile.Helpers
{
    public class LogRequestAndResponseHandler : DelegatingHandler
    {
        private string GetUserHostAddress(HttpRequestMessage request = null)
        {
            if (request != null && request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }
            if (request != null && request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                return
                    (request.Properties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty)?.Address;
            }
            return HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
           HttpRequestMessage request, CancellationToken cancellationToken)
        {

            var requestHeaders = new HashSet<DTOHeaderKey>();

            request.Headers.ForEach(item =>
            {
                requestHeaders.Add(new DTOHeaderKey()
                {
                    Key = item.Key,
                    Value = item.Value?.ToHashSet()
                });
            });


            var method = request.Method.Method;

            var requestUri = request.RequestUri.ToString();
            var pathandQuery = request.RequestUri.PathAndQuery;

            IEnumerable<string> originValues;
            request.Headers.TryGetValues("Origin", out originValues);


            var userHostAddress = GetUserHostAddress(request);
            var userHostName = HttpContext.Current.Request.UserHostName;

            #region Authorized User
            string isAuthExist = HttpContext.Current.Request.Headers["Authorization"];
            DTOUserMaster currentUser = new DTOUserMaster();
            if (!string.IsNullOrEmpty(isAuthExist))
            {
                var headerToken = HttpContext.Current.Request.Headers.GetValues("Authorization").FirstOrDefault();
                string[] tokensValues = headerToken.Split(':');
                currentUser = new CredentialValidation().CheckCredential(tokensValues[0], tokensValues[1]);
            }

            #endregion

            var req = new DTOClientRequest()
            {
                UserHostName = userHostName,
                UserHostAddress = userHostAddress,
                OriginValues = originValues.ToHashSet(),
                RequestUri = requestUri,
                PathAndQuery = pathandQuery,
                MethodType = method,
                Headers = requestHeaders,
                UserId = currentUser.Id == 0 ? 1 : currentUser.Id
            };

            var requestBody = await request.Content.ReadAsStringAsync();
            req.RequestBody = requestBody;


            HttpResponseMessage result;
            try
            {
                // let other handlers process the request
                result = await base.SendAsync(request, cancellationToken);

                if (result.Content != null)
                {
                    //var ex = result.Content.ReadAsAsync<Exception>(cancellationToken);

                    var lll = result.SerializeJson();

                    // once response body is ready, log it
                    var resStatus = result.StatusCode.ToString();

                    req.Status = resStatus;

                    var responseBody = await result.Content.ReadAsStringAsync();
                    req.ResponseBody = responseBody;
                }
                else
                {
                    req.Status = "Content Null";
                }
            }
            catch (Exception e)
            {
                result = null;
                req.Status = "Null Response";
            }

            ServiceLogger.LogRequestAndResponse(req);

            return result;
        }
    }
}