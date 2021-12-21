using System;
using System.Linq;
using HyperPay.Shared.Dtos;
using System.Collections.Generic;
using HyperPay.Shared.Models;

namespace HyperPay.Mobile.Helpers
{
    public static class ServiceLogger
    {

        public static void LogRequestAndResponse(DTOClientRequest clientRequest)
        {
            try
            {
                var _hedear = new HashSet<DTOHeaderKey>();
                var _xForwardedFor = new HashSet<string>();

                clientRequest.Headers.ForEach(item =>
                {
                    if (item.Key.ToLower() == "x-forwarded-for")
                        _xForwardedFor = item.Value;

                    _hedear.Add(new DTOHeaderKey()
                    {
                        Key = item.Key,
                        Value = item.Value
                    });
                }
                );

                var userHostName = clientRequest.UserHostName;
                var userHostAddress = clientRequest.UserHostAddress;
                var OriginValues = clientRequest.OriginValues.SerializeJson();
                var RequestUri = clientRequest.RequestUri;
                var PathandQuery = clientRequest.PathAndQuery;
                var MethodType = clientRequest.MethodType.Length >= 10
                                ? clientRequest.MethodType.Substring(0, 10)
                                : clientRequest.MethodType;
                var Headers = _hedear.SerializeJson();
                var xForwardedFor = _xForwardedFor.SerializeJson();
                var RequestBody = clientRequest.RequestBody;
                var ResponseBody = clientRequest.ResponseBody;
                var Status = clientRequest.Status;
                var AuthUserId = clientRequest.UserId;

                LogRequestAndResponseToDb(userHostName, userHostAddress, OriginValues, RequestUri, PathandQuery, MethodType, Headers, xForwardedFor, RequestBody, ResponseBody, Status, AuthUserId);
            }
            catch (Exception ex)
            {

            }
        }

        public static void LogRequestAndResponseToDb(string userHostName, string userHostAddress, string origin, string requestUri, string pathAndQuery, string methodType, string headers, string xForwardedFor, string requestBody, string responseBody, string status, int userId)
        {
            using (HyperPayContext _ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_CreateLogger '{userHostName}','{userHostAddress}','{origin}','{requestUri}','{pathAndQuery}','{methodType}','{headers}','{xForwardedFor}','{requestBody}','{responseBody?.Replace("'", "\"")}','{status}',{userId}";
                _ctx.Database.SqlQuery<string>(query).FirstOrDefault();
            }
        }
    }
}