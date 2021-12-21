using HyperPay.Service.Helper;
using HyperPay.Shared.Dtos;
using HyperPay.Shared.Models;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace HyperPay.Service
{
    public class SetupServer : ISetupServer
    {
        public Task<DTOCheckOutResponse> CheckoutsAsync(DTOMerchand model, DTOUserMaster user)
        {
            #region Insert Checkout
            long recordId;
            string entityId = FindEntityIdFromPaymentMethod(model.PaymentMethod).Result;

            using (var ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_InsertCheckout {model.MerchantTransactionId},{model.Amount} ,'{model.Currency}'," +
                    $" {model.PaymentType}, '{model.PaymentMethod}','{entityId}','{model.Email}','{model.GivenName}', " +
                    $"'{model.SureName}', '{model.PostCode}', '{model.Street}', '{model.City}', '{model.State}'," +
                    $"'{model.Country}', '{model.Phone}', '{model.Lang}', '{model.SAPTCOCustomParameters?.CustomParameter1}', '{model.SAPTCOCustomParameters?.CustomParameter2}', {user.Id}, {user.SystemId}";

                recordId = (long)ctx.Database.SqlQuery<decimal>(query).FirstOrDefault();
            }
            #endregion

            var client = new RestClient(ConfigurationManager.AppSettings["oppwaCheckOutUrl"]);
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", ConfigurationManager.AppSettings["bearerToken"]);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("entityId", entityId);
            request.AddParameter("amount", model.Amount);
            request.AddParameter("currency", model.Currency);
            request.AddParameter("paymentType", model.PaymentType);

            if (ConfigurationManager.AppSettings["Environment"].ToLower() == "test")
                request.AddParameter("testMode", FindTestModeFromPaymentMethod(model.PaymentMethod).Result);

            request.AddParameter("merchantTransactionId", model.MerchantTransactionId);
            request.AddParameter("customer.email", model.Email);
            request.AddParameter("billing.street1", model.Street);
            request.AddParameter("billing.city", model.City);
            request.AddParameter("billing.state", model.Street);
            request.AddParameter("billing.country", model.Country);
            request.AddParameter("billing.postcode", model.PostCode);
            request.AddParameter("customer.givenName", model.GivenName);
            request.AddParameter("customer.surname", model.SureName);
            IRestResponse response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<DTOCheckOutResponse>(response.Content);

            #region Update Checkout
            using (var ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_UpdateCheckout {recordId}, '{response.Content.Replace("'", "''")}', '{result.result.code}' ,'{result.result.description}', '{result.ndc}'," +
                    $" '{result.id}','{result.timestamp}'";

                string res = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
            }
            #endregion


            return Task.FromResult(result);
        }

        public Task<DTOInvoiceErrorResponse> ErrorSimpleInvoiceResponse(string response)
        {
            var result = JsonConvert.DeserializeObject<DTOInvoiceErrorResponse>(response);
            return Task.FromResult(result);
        }

        public Task<string> CreatePaymentForm(string checkoutId, string returnURL)
        {
            string dataBrands;

            string oppwaJsUrl = ConfigurationManager.AppSettings["oppwaJsUrl"];

            #region GetPaymentMethodByCheckoutId

            string paymentMethod;

            using (var ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_GetPaymentMethodByCheckoutId '{checkoutId}'";

                paymentMethod = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
            }
            #endregion

            if (paymentMethod.ToUpper().Contains("MADA"))
            {
                dataBrands = "MADA";
            }
            else if (paymentMethod.ToUpper().Contains("VISA") || paymentMethod.ToUpper().Contains("MASTER"))
            {
                dataBrands = "VISA MASTER";
            }
            else
            {
                return Task.FromResult(string.Empty);
            }


            string drawForm = string.Format(@"<html>
                                                <head>
                                                    <title>Create the payment form</title>
                                                    <script src='{0}?checkoutId={1}'></script>
	                                                <script>
                                                        var wpwlOptions = {{ style: 'plain' }}
                                                    </script>
                                                </head>
                                                <body>
                                                    <form action='{2}' class='paymentWidgets' data-brands='{3}'></form>
                                                </body>
                                              </html>".Replace("'", "\""), oppwaJsUrl, checkoutId, returnURL, dataBrands);

            return Task.FromResult(drawForm);
        }

        public Task<string> FindEntityIdFromPaymentMethod(string paymentMethod)
        {
            string entityId = string.Empty;

            if (paymentMethod.ToUpper().Contains("VISA") || paymentMethod.ToUpper().Contains("MASTER"))
            {
                entityId = PaymentTypeWrapper.VISA_MASTER.Value;
            }
            else if (paymentMethod.ToUpper().Contains("MADA"))
            {
                entityId = PaymentTypeWrapper.MADA.Value;
            }

            return Task.FromResult(entityId);
        }

        public Task<string> FindTestModeFromPaymentMethod(string paymentMethod)
        {
            string testMode = string.Empty;

            if (paymentMethod.ToUpper().Contains("VISA") || paymentMethod.ToUpper().Contains("MASTER"))
            {
                testMode = "EXTERNAL";
            }
            else if (paymentMethod.ToUpper().Contains("MADA"))
            {
                testMode = "INTERNAL";
            }

            return Task.FromResult(testMode);
        }

        public Task<string> GenerateSimpleInvoice(DTOSimpleInvoice simpleInvoice, DTOUserMaster user)
        {
            long OwnerSystemMerchantInvoiceNumber = long.Parse(simpleInvoice.merchant_invoice_number),
            HyperPayMerchantInvoiceNumber = GetNextHyperPayInvoiceNumber().Result;

            //Override by unique invoice number
            simpleInvoice.merchant_invoice_number = HyperPayMerchantInvoiceNumber.ToString();

            //Override the expiration date
            simpleInvoice.expiration_date = DateTime.ParseExact(simpleInvoice.expiration_date, "yyyy-MM-dd HH:mm:ss", null).AddMinutes(user.InvoiceExpirationInMinutes).ToString("yyyy-MM-dd HH:mm:ss");


            string customParameter1 = simpleInvoice.SAPTCOCustomParameters?.CustomParameter1;
            string customParameter2 = simpleInvoice.SAPTCOCustomParameters?.CustomParameter2;
            simpleInvoice.SAPTCOCustomParameters = null;


            #region Insert Simple Invoice
            long recordId;

            using (var ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_InsertSimpleInvoice {HyperPayMerchantInvoiceNumber},{OwnerSystemMerchantInvoiceNumber}, {decimal.Parse(simpleInvoice.amount)}," +
                    $"'{simpleInvoice.currency}', '{simpleInvoice.payment_type}','{simpleInvoice.name}','{simpleInvoice.email}','{simpleInvoice.phone}', " +
                    $"'{simpleInvoice.lang}', '{simpleInvoice.expiration_date}', '{customParameter1}', '{customParameter2}'," +
                    $"'{JsonConvert.SerializeObject(simpleInvoice)}', {user.Id}, {user.SystemId}";

                recordId = (long)ctx.Database.SqlQuery<decimal>(query).FirstOrDefault();
            }
            #endregion


            var client = new RestClient(ConfigurationManager.AppSettings["simpleInvoice"]);
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", ConfigurationManager.AppSettings["hyperBillToken"]);
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("application/json", JsonConvert.SerializeObject(simpleInvoice), ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);


            #region Update Simple Invoice
            using (var ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_UpdateInvoice {recordId}, '{response.Content}'";

                string updateResult = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
            }
            #endregion

            return Task.FromResult(response.Content);
        }

        public Task<bool> ProcessPmtNotification(string checkoutId, DTOPaymentStatusInfo dTOPaymentStatusInfo)
        {
            DTOCheckoutInfo checkoutInfo = GetCheckoutInfo(checkoutId).Result;

            if (checkoutInfo.CheckoutId != "-1")
            {
                //Existing checkout

                if (checkoutInfo.Amount == decimal.Parse(dTOPaymentStatusInfo.amount) &&
                    checkoutInfo.Currency == dTOPaymentStatusInfo.currency &&
                    checkoutInfo.PaymentType == dTOPaymentStatusInfo.paymentType)
                {

                    if (checkoutInfo.OwnerSystemDeliveryStatus == -1)
                    {
                        //No record in the payment delivery table.

                        string insertRes = InsertCheckoutPaymentDelivery(dTOPaymentStatusInfo.ndc).Result;

                        if (insertRes.ToLower() == "success" || insertRes.ToLower() == "record_already_exist")
                        {

                            DTOCheckoutPmtNotificationInfo dTOPmtNotificationInfo = new DTOCheckoutPmtNotificationInfo
                            {
                                PaymentId = dTOPaymentStatusInfo.id,
                                CheckoutId = dTOPaymentStatusInfo.ndc,
                                MerchantTransactionId = long.Parse(dTOPaymentStatusInfo.merchantTransactionId),
                                Amount = decimal.Parse(dTOPaymentStatusInfo.amount),
                                Currency = dTOPaymentStatusInfo.currency,
                                PaymentMethod = dTOPaymentStatusInfo.paymentBrand,
                                PaymentProcessingDate = DateTime.ParseExact(dTOPaymentStatusInfo.timestamp, "yyyy-MM-dd HH:mm:ss+0000", null).ToString("dd/MM/yyyy HH:mm:ss"),
                                SAPTCOCustomParameters = new SAPTCOCustomParameters
                                {
                                    CustomParameter1 = checkoutInfo.CustomParameter1,
                                    CustomParameter2 = checkoutInfo.CustomParameter2
                                }
                            };

                            if (!string.IsNullOrEmpty(checkoutInfo.HookNotificationTicketsUrl))
                                dTOPmtNotificationInfo.SAPTCOCustomParameters.TicketsURL = string.Format(checkoutInfo.HookNotificationTicketsUrl, checkoutInfo.MerchantTransactionId, checkoutInfo.Phone, checkoutInfo.Lang);


                            bool pmtNotificationProcessedSuccessfully = ProcessCheckoutPmtNotificationToOwnerSystem(checkoutInfo.SystemId,
                                                                        checkoutInfo.OwnerSystemPmtNotificationURL, checkoutInfo.OwnerSystemPmtNotificationUserName,
                                                                        checkoutInfo.OwnerSystemPmtNotificationPassword, dTOPmtNotificationInfo).Result;

                            if (pmtNotificationProcessedSuccessfully && !string.IsNullOrEmpty(checkoutInfo.OwnerChannelHookNotificationURL))
                            {
                                if (checkoutInfo.OwnerChannelHookNotificationURL.Contains("{"))
                                    checkoutInfo.OwnerChannelHookNotificationURL = string.Format(checkoutInfo.OwnerChannelHookNotificationURL, checkoutInfo.CustomParameter1);

                                ProcessCheckoutPmtNotificationToOwnerChannel(checkoutInfo.OwnerChannelHookNotificationURL, checkoutInfo.OwnerChannelHookNotificationUserName,
                                        checkoutInfo.OwnerChannelHookNotificationPassword, dTOPmtNotificationInfo);
                            }

                            if (pmtNotificationProcessedSuccessfully)
                            {
                                return Task.FromResult(true);
                            }
                            else
                            {
                                return Task.FromResult(false);
                            }
                        }
                        else
                        {
                            return Task.FromResult(false);
                        }
                    }
                    else if (checkoutInfo.OwnerSystemDeliveryStatus == (int)PaymentDeliveryStatus.New ||
                             checkoutInfo.OwnerSystemDeliveryStatus == (int)PaymentDeliveryStatus.UnderProcessing)
                    {
                        //There is another thread trying to deliver the payment now.
                        Thread.Sleep(4000);

                        int PmtDeliveryStatusId = GetCheckoutPaymentDelivery(checkoutId).Result;

                        if (PmtDeliveryStatusId == (int)PaymentDeliveryStatus.Delivered)
                        {
                            //Payment delivered, so no need to make any action.
                            return Task.FromResult(true);
                        }
                        else
                        {
                            Thread.Sleep(5000);

                            if (PmtDeliveryStatusId == (int)PaymentDeliveryStatus.Delivered)
                            {
                                //Payment delivered, so no need to make any action.
                                return Task.FromResult(true);
                            }
                            else
                            {
                                return Task.FromResult(false);
                            }
                        }
                    }
                    else if (checkoutInfo.OwnerSystemDeliveryStatus == (int)PaymentDeliveryStatus.Delivered)
                    {
                        //Payment delivered, so no need to make any action.
                        return Task.FromResult(true);
                    }
                    else
                    {
                        return Task.FromResult(false);
                    }
                }
                else
                {
                    //ToDo: We may send email notification to the admin that one checkout session is paid in Hyperpay but with different amount or different currency.
                    return Task.FromResult(false);
                }
            }
            else
            {
                //ToDo: We may send email notification to the admin that one checkout session is paid in Hyperpay but it is not exist in our database (checkout table).

                return Task.FromResult(false);
            }
        }

        public Task<DTONotifyOwnerSystemByPaymentRs> NotifyOwnerSystemByPayment(string PmtNotificationURL, string PmtNotificationUserName, string PmtNotificationPassword, object dTOPmtNotificationInfo)
        {
            try
            {
                var client = new RestClient(PmtNotificationURL);
                client.Timeout = 10000;  //10 seconds
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", "Basic " + Convert.ToBase64String(new ASCIIEncoding().GetBytes(String.Format("{0}:{1}", PmtNotificationUserName, PmtNotificationPassword))));
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", JsonConvert.SerializeObject(dTOPmtNotificationInfo), ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    DTONotifyOwnerSystemByPaymentRs dTONotifyOwnerSystemByPaymentRs = new DTONotifyOwnerSystemByPaymentRs
                    {
                        IsDelivered = true,
                        OwnerSystemResponse = response.Content
                    };

                    return Task.FromResult(dTONotifyOwnerSystemByPaymentRs);
                }
                else
                {
                    DTONotifyOwnerSystemByPaymentRs dTONotifyOwnerSystemByPaymentRs = new DTONotifyOwnerSystemByPaymentRs
                    {
                        IsDelivered = false,
                        OwnerSystemResponse = response.Content
                    };

                    return Task.FromResult(dTONotifyOwnerSystemByPaymentRs);
                }
            }
            catch (Exception ex)
            {
                DTONotifyOwnerSystemByPaymentRs dTONotifyOwnerSystemByPaymentRs = new DTONotifyOwnerSystemByPaymentRs
                {
                    IsDelivered = false,
                    OwnerSystemResponse = "Exception delivering the payment to Owner system: " + ex.Message + " " +
                    ex.InnerException?.Message + " " + ex.InnerException?.InnerException?.Message
                };

                return Task.FromResult(dTONotifyOwnerSystemByPaymentRs);
            }
        }

        public Task<DTONotifyOwnerChannelByPaymentRs> NotifyOwnerChannelByPayment(string PmtNotificationURL, string PmtNotificationUserName, string PmtNotificationPassword, object dTOPmtNotificationInfo)
        {
            try
            {
                var client = new RestClient(PmtNotificationURL);
                client.Timeout = 10000;  //10 seconds
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", "Basic " + Convert.ToBase64String(new ASCIIEncoding().GetBytes(String.Format("{0}:{1}", PmtNotificationUserName, PmtNotificationPassword))));
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", JsonConvert.SerializeObject(dTOPmtNotificationInfo), ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    DTONotifyOwnerChannelByPaymentRs dTONotifyOwnerChannelByPaymentRs = new DTONotifyOwnerChannelByPaymentRs
                    {
                        IsDelivered = true,
                        OwnerChannelResponse = response.Content
                    };

                    return Task.FromResult(dTONotifyOwnerChannelByPaymentRs);
                }
                else
                {
                    DTONotifyOwnerChannelByPaymentRs dTONotifyOwnerChannelByPaymentRs = new DTONotifyOwnerChannelByPaymentRs
                    {
                        IsDelivered = false,
                        OwnerChannelResponse = response.Content
                    };

                    return Task.FromResult(dTONotifyOwnerChannelByPaymentRs);
                }
            }
            catch (Exception ex)
            {
                DTONotifyOwnerChannelByPaymentRs dTONotifyOwnerChannelByPaymentRs = new DTONotifyOwnerChannelByPaymentRs
                {
                    IsDelivered = false,
                    OwnerChannelResponse = "Exception delivering the payment to Owner Channel: " + ex.Message + " " +
                    ex.InnerException?.Message + " " + ex.InnerException?.InnerException?.Message
                };

                return Task.FromResult(dTONotifyOwnerChannelByPaymentRs);
            }
        }

        public Task<bool> ProcessCheckoutPmtNotificationToOwnerSystem(int systemId, string pmtNotificationURL, string pmtNotificationUserName, string pmtNotificationPassword,
                                                              DTOCheckoutPmtNotificationInfo dTOPmtNotificationInfo)
        {

            #region Update Checkout Payment Delivery As UnderProcessing
            string updateRes;

            using (var ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_UpdateCheckoutPaymentDeliveryToSystem '{dTOPmtNotificationInfo.CheckoutId}' ," +
                    $"{(int)PaymentDeliveryStatus.UnderProcessing}, '{JsonConvert.SerializeObject(dTOPmtNotificationInfo)}', ''";

                updateRes = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
            }
            #endregion


            if (updateRes.ToLower() == "success")
            {
                if (systemId == int.Parse(ConfigurationManager.AppSettings["TRSystemId"]))
                {

                    DTOTRPaid dTOTRPaid = new DTOTRPaid
                    {
                        p_saptco_pnr = dTOPmtNotificationInfo.MerchantTransactionId,
                        p_payment_method_id = dTOPmtNotificationInfo.PaymentMethod == "MADA" ? long.Parse(ConfigurationManager.AppSettings["MADAPaymentMethodId"]) : long.Parse(ConfigurationManager.AppSettings["CreditCardPaymentMethodId"]),
                        p_total_paid_amount = dTOPmtNotificationInfo.Amount,
                        p_transaction_id = dTOPmtNotificationInfo.MerchantTransactionId.ToString(),
                        p_payment_process_date = dTOPmtNotificationInfo.PaymentProcessingDate,
                        p_is_paying = 1
                    };

                    DTOTRPaidResult dTOTRPaidResult = TRPaid(dTOTRPaid).Result;

                    if (dTOTRPaidResult?.p_rtrn_cd == "000000" || dTOTRPaidResult?.p_rtrn_cd == "2003040005") //2003040005  Tickets are expired
                    {
                        #region Update Checkout Payment Delivery As Delivered
                        string updateResponse;

                        using (var ctx = new HyperPayContext())
                        {
                            string query = $"EXECUTE SP_UpdateCheckoutPaymentDeliveryToSystem '{dTOPmtNotificationInfo.CheckoutId}' ," +
                                $"{(int)PaymentDeliveryStatus.Delivered}, '{JsonConvert.SerializeObject(dTOPmtNotificationInfo)}', '{JsonConvert.SerializeObject(dTOTRPaidResult)}'";

                            updateResponse = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
                        }
                        #endregion

                        if (updateResponse.ToLower() == "success")
                        {
                            return Task.FromResult(true);
                        }
                        else
                        {
                            return Task.FromResult(false);
                        }
                    }
                    else
                    {
                        #region Update Checkout Payment Delivery As Failed
                        string updateResponse;

                        using (var ctx = new HyperPayContext())
                        {
                            string query = $"EXECUTE SP_UpdateCheckoutPaymentDeliveryToSystem '{dTOPmtNotificationInfo.CheckoutId}' ," +
                                $"{(int)PaymentDeliveryStatus.Failed}, '{JsonConvert.SerializeObject(dTOPmtNotificationInfo)}', '{JsonConvert.SerializeObject(dTOTRPaidResult)}'";

                            updateResponse = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
                        }
                        #endregion

                        return Task.FromResult(false);
                    }
                }
                else
                {
                    var ownerSystemNotificationResult = NotifyOwnerSystemByPayment(pmtNotificationURL, pmtNotificationUserName, pmtNotificationPassword, dTOPmtNotificationInfo).Result;

                    if (ownerSystemNotificationResult.IsDelivered)
                    {
                        #region Update Checkout Payment Delivery As Delivered
                        string updateResponse;

                        using (var ctx = new HyperPayContext())
                        {
                            string query = $"EXECUTE SP_UpdateCheckoutPaymentDeliveryToSystem '{dTOPmtNotificationInfo.CheckoutId}' ," +
                                $"{(int)PaymentDeliveryStatus.Delivered}, '{JsonConvert.SerializeObject(dTOPmtNotificationInfo)}', '{ownerSystemNotificationResult.OwnerSystemResponse}'";

                            updateResponse = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
                        }
                        #endregion

                        if (updateResponse.ToLower() == "success")
                        {
                            return Task.FromResult(true);
                        }
                        else
                        {
                            return Task.FromResult(false);
                        }
                    }
                    else
                    {
                        #region Update Checkout Payment Delivery As Failed
                        string updateResponse;

                        using (var ctx = new HyperPayContext())
                        {
                            string query = $"EXECUTE SP_UpdateCheckoutPaymentDeliveryToSystem '{dTOPmtNotificationInfo.CheckoutId}' ," +
                                $"{(int)PaymentDeliveryStatus.Failed}, '{JsonConvert.SerializeObject(dTOPmtNotificationInfo)}', '{ownerSystemNotificationResult.OwnerSystemResponse}'";

                            updateResponse = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
                        }
                        #endregion

                        return Task.FromResult(false);
                    }
                }
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> ProcessCheckoutPmtNotificationToOwnerChannel(string pmtNotificationURL, string pmtNotificationUserName, string pmtNotificationPassword,
                                      DTOCheckoutPmtNotificationInfo dTOPmtNotificationInfo)
        {

            #region Update Checkout Payment Delivery As UnderProcessing
            string updateRes;

            using (var ctx = new HyperPayContext())
            {
                string query = $"EXEC SP_UpdateCheckoutPaymentDeliveryToChannel '{dTOPmtNotificationInfo.CheckoutId}' ," +
                    $"{(int)PaymentDeliveryStatus.UnderProcessing}, '{JsonConvert.SerializeObject(dTOPmtNotificationInfo)}', ''";

                updateRes = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
            }
            #endregion


            if (updateRes.ToLower() == "success")
            {
                var ownerChannelNotificationResult = NotifyOwnerChannelByPayment(pmtNotificationURL, pmtNotificationUserName, pmtNotificationPassword, dTOPmtNotificationInfo).Result;

                if (ownerChannelNotificationResult.IsDelivered)
                {
                    #region Update Checkout Payment Delivery As Delivered
                    string updateResponse;

                    using (var ctx = new HyperPayContext())
                    {
                        string query = $"EXECUTE SP_UpdateCheckoutPaymentDeliveryToChannel '{dTOPmtNotificationInfo.CheckoutId}' ," +
                            $"{(int)PaymentDeliveryStatus.Delivered}, '{JsonConvert.SerializeObject(dTOPmtNotificationInfo)}', '{ownerChannelNotificationResult.OwnerChannelResponse}'";

                        updateResponse = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
                    }
                    #endregion

                    if (updateResponse.ToLower() == "success")
                    {
                        return Task.FromResult(true);
                    }
                    else
                    {
                        return Task.FromResult(false);
                    }
                }
                else
                {
                    #region Update Checkout Payment Delivery As Failed
                    string updateResponse;

                    using (var ctx = new HyperPayContext())
                    {
                        string query = $"EXECUTE SP_UpdateCheckoutPaymentDeliveryToChannel '{dTOPmtNotificationInfo.CheckoutId}' ," +
                            $"{(int)PaymentDeliveryStatus.Failed}, '{JsonConvert.SerializeObject(dTOPmtNotificationInfo)}', '{ownerChannelNotificationResult.OwnerChannelResponse}'";

                        updateResponse = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
                    }
                    #endregion

                    return Task.FromResult(false);
                }
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> ProcessInvoicePmtNotificationToOwnerSystem(int systemId, string pmtNotificationURL, string pmtNotificationUserName, string pmtNotificationPassword,
                                              DTOInvoicePmtNotificationInfo dTOPmtNotificationInfo)
        {

            #region Update Invoice Payment Delivery As UnderProcessing
            string updateRes;

            using (var ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_UpdateInvoicePaymentDeliveryToSystem {dTOPmtNotificationInfo.HyperPayMerchantInvoiceNumber} ," +
                    $"{(int)PaymentDeliveryStatus.UnderProcessing}, '{JsonConvert.SerializeObject(dTOPmtNotificationInfo)}', ''";

                updateRes = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
            }
            #endregion


            if (updateRes.ToLower() == "success")
            {
                if (systemId == int.Parse(ConfigurationManager.AppSettings["TRSystemId"]))
                {

                    DTOTRPaid dTOTRPaid = new DTOTRPaid
                    {
                        p_saptco_pnr = dTOPmtNotificationInfo.OwnerSystemMerchantInvoiceNumber,
                        p_payment_method_id = dTOPmtNotificationInfo.PaymentMethod == "MADA" ? 13 : 2,
                        p_total_paid_amount = dTOPmtNotificationInfo.Amount,
                        p_transaction_id = dTOPmtNotificationInfo.HyperPayMerchantInvoiceNumber.ToString(),
                        p_payment_process_date = dTOPmtNotificationInfo.PaymentProcessingDate,
                        p_is_paying = 1
                    };

                    DTOTRPaidResult dTOTRPaidResult = TRPaid(dTOTRPaid).Result;

                    if (dTOTRPaidResult?.p_rtrn_cd == "000000" || dTOTRPaidResult?.p_rtrn_cd == "2003040005") //2003040005  Tickets are expired
                    {
                        #region Update Invoice Payment Delivery As Delivered
                        string updateResponse;

                        using (var ctx = new HyperPayContext())
                        {
                            string query = $"EXECUTE SP_UpdateInvoicePaymentDeliveryToSystem {dTOPmtNotificationInfo.HyperPayMerchantInvoiceNumber} ," +
                                $"{(int)PaymentDeliveryStatus.Delivered}, '{JsonConvert.SerializeObject(dTOPmtNotificationInfo)}', '{JsonConvert.SerializeObject(dTOTRPaidResult)}'";

                            updateResponse = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
                        }
                        #endregion

                        if (updateResponse.ToLower() == "success")
                        {
                            return Task.FromResult(true);
                        }
                        else
                        {
                            return Task.FromResult(false);
                        }
                    }
                    else
                    {
                        #region Update Invoice Payment Delivery As Failed
                        string updateResponse;

                        using (var ctx = new HyperPayContext())
                        {
                            string query = $"EXECUTE SP_UpdateInvoicePaymentDeliveryToSystem {dTOPmtNotificationInfo.HyperPayMerchantInvoiceNumber} ," +
                                $"{(int)PaymentDeliveryStatus.Failed}, '{JsonConvert.SerializeObject(dTOPmtNotificationInfo)}', '{JsonConvert.SerializeObject(dTOTRPaidResult)}'";

                            updateResponse = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
                        }
                        #endregion

                        return Task.FromResult(false);
                    }
                }
                else
                {
                    var ownerSystemNotificationResult = NotifyOwnerSystemByPayment(pmtNotificationURL, pmtNotificationUserName, pmtNotificationPassword, dTOPmtNotificationInfo).Result;

                    if (ownerSystemNotificationResult.IsDelivered)
                    {
                        #region Update Invoice Payment Delivery As Delivered
                        string updateResponse;

                        using (var ctx = new HyperPayContext())
                        {
                            string query = $"EXECUTE SP_UpdateInvoicePaymentDeliveryToSystem {dTOPmtNotificationInfo.HyperPayMerchantInvoiceNumber} ," +
                                $"{(int)PaymentDeliveryStatus.Delivered}, '{JsonConvert.SerializeObject(dTOPmtNotificationInfo)}', '{JsonConvert.SerializeObject(ownerSystemNotificationResult.OwnerSystemResponse)}'";

                            updateResponse = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
                        }
                        #endregion

                        if (updateResponse.ToLower() == "success")
                        {
                            return Task.FromResult(true);
                        }
                        else
                        {
                            return Task.FromResult(false);
                        }
                    }
                    else
                    {
                        #region Update Invoice Payment Delivery As Failed
                        string updateResponse;

                        using (var ctx = new HyperPayContext())
                        {
                            string query = $"EXECUTE SP_UpdateInvoicePaymentDeliveryToSystem {dTOPmtNotificationInfo.HyperPayMerchantInvoiceNumber} ," +
                                $"{(int)PaymentDeliveryStatus.Failed}, '{JsonConvert.SerializeObject(dTOPmtNotificationInfo)}', '{JsonConvert.SerializeObject(ownerSystemNotificationResult.OwnerSystemResponse)}'";

                            updateResponse = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
                        }
                        #endregion

                        return Task.FromResult(false);
                    }
                }
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> ProcessInvoicePmtNotificationToOwnerChannel(string pmtNotificationURL, string pmtNotificationUserName, string pmtNotificationPassword,
                                              DTOInvoicePmtNotificationInfo dTOPmtNotificationInfo)
        {

            #region Update Invoice Payment Delivery As UnderProcessing
            string updateRes;

            using (var ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_UpdateInvoicePaymentDeliveryToChannel {dTOPmtNotificationInfo.HyperPayMerchantInvoiceNumber} ," +
                    $"{(int)PaymentDeliveryStatus.UnderProcessing}, '{JsonConvert.SerializeObject(dTOPmtNotificationInfo)}', ''";

                updateRes = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
            }
            #endregion


            if (updateRes.ToLower() == "success")
            {
                var ownerChannelNotificationResult = NotifyOwnerChannelByPayment(pmtNotificationURL, pmtNotificationUserName, pmtNotificationPassword, dTOPmtNotificationInfo).Result;

                if (ownerChannelNotificationResult.IsDelivered)
                {
                    #region Update Invoice Payment Delivery As Delivered
                    string updateResponse;

                    using (var ctx = new HyperPayContext())
                    {
                        string query = $"EXECUTE SP_UpdateInvoicePaymentDeliveryToChannel {dTOPmtNotificationInfo.HyperPayMerchantInvoiceNumber} ," +
                            $"{(int)PaymentDeliveryStatus.Delivered}, '{JsonConvert.SerializeObject(dTOPmtNotificationInfo)}', '{JsonConvert.SerializeObject(ownerChannelNotificationResult.OwnerChannelResponse)}'";

                        updateResponse = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
                    }
                    #endregion

                    if (updateResponse.ToLower() == "success")
                    {
                        return Task.FromResult(true);
                    }
                    else
                    {
                        return Task.FromResult(false);
                    }
                }
                else
                {
                    #region Update Invoice Payment Delivery As Failed
                    string updateResponse;

                    using (var ctx = new HyperPayContext())
                    {
                        string query = $"EXECUTE SP_UpdateInvoicePaymentDeliveryToChannel {dTOPmtNotificationInfo.HyperPayMerchantInvoiceNumber} ," +
                            $"{(int)PaymentDeliveryStatus.Failed}, '{JsonConvert.SerializeObject(dTOPmtNotificationInfo)}', '{JsonConvert.SerializeObject(ownerChannelNotificationResult.OwnerChannelResponse)}'";

                        updateResponse = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
                    }
                    #endregion

                    return Task.FromResult(false);
                }
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> CheckoutHookNotification(DTOWebhookInfo dTOWebhookInfo)
        {
            DTOCheckoutInfo checkoutInfo = GetCheckoutInfo(dTOWebhookInfo.payload.ndc).Result;

            if (checkoutInfo.CheckoutId != "-1")
            {
                //Hook notification for existing checkout

                if (checkoutInfo.Amount == decimal.Parse(dTOWebhookInfo.payload.amount) &&
                    checkoutInfo.Currency == dTOWebhookInfo.payload.currency &&
                    checkoutInfo.PaymentType == dTOWebhookInfo.payload.paymentType)
                {

                    if (checkoutInfo.OwnerSystemDeliveryStatus == -1)
                    {
                        //No record in the payment delivery table.

                        string insertRes = InsertCheckoutPaymentDelivery(dTOWebhookInfo.payload.ndc).Result;

                        if (insertRes.ToLower() == "success")
                        {

                            DTOCheckoutPmtNotificationInfo dTOPmtNotificationInfo = new DTOCheckoutPmtNotificationInfo
                            {
                                PaymentId = dTOWebhookInfo.payload.id,
                                CheckoutId = dTOWebhookInfo.payload.ndc,
                                MerchantTransactionId = long.Parse(dTOWebhookInfo.payload.merchantTransactionId),
                                Amount = decimal.Parse(dTOWebhookInfo.payload.amount),
                                Currency = dTOWebhookInfo.payload.currency,
                                PaymentMethod = dTOWebhookInfo.payload.paymentBrand,
                                PaymentProcessingDate = DateTime.ParseExact(dTOWebhookInfo.payload.timestamp, "yyyy-MM-dd HH:mm:ss+0000", null).ToString("dd/MM/yyyy HH:mm:ss"),
                                SAPTCOCustomParameters = new SAPTCOCustomParameters
                                {
                                    CustomParameter1 = checkoutInfo.CustomParameter1,
                                    CustomParameter2 = checkoutInfo.CustomParameter2
                                }
                            };

                            if (!string.IsNullOrEmpty(checkoutInfo.HookNotificationTicketsUrl))
                                dTOPmtNotificationInfo.SAPTCOCustomParameters.TicketsURL = string.Format(checkoutInfo.HookNotificationTicketsUrl, checkoutInfo.MerchantTransactionId, checkoutInfo.Phone, checkoutInfo.Lang);


                            bool pmtNotificationProcessedSuccessfully = ProcessCheckoutPmtNotificationToOwnerSystem(checkoutInfo.SystemId,
                                                                        checkoutInfo.OwnerSystemPmtNotificationURL, checkoutInfo.OwnerSystemPmtNotificationUserName,
                                                                        checkoutInfo.OwnerSystemPmtNotificationPassword, dTOPmtNotificationInfo).Result;

                            if (pmtNotificationProcessedSuccessfully && !string.IsNullOrEmpty(checkoutInfo.OwnerChannelHookNotificationURL))
                            {
                                if (checkoutInfo.OwnerChannelHookNotificationURL.Contains("{"))
                                    checkoutInfo.OwnerChannelHookNotificationURL = string.Format(checkoutInfo.OwnerChannelHookNotificationURL, checkoutInfo.CustomParameter1);

                                ProcessCheckoutPmtNotificationToOwnerChannel(checkoutInfo.OwnerChannelHookNotificationURL, checkoutInfo.OwnerChannelHookNotificationUserName,
                                        checkoutInfo.OwnerChannelHookNotificationPassword, dTOPmtNotificationInfo);
                            }

                            return Task.FromResult(true);
                        }
                        else if (insertRes.ToLower() == "record_already_exist")
                        {
                            return Task.FromResult(true);
                        }
                        else
                        {
                            return Task.FromResult(false);
                        }
                    }
                    else
                    {
                        //As Long As there is a record in the payment delivery table, SAPTCO will return true to Hyperpay.
                        //It is the responsiblity of Hangfire retry job to retry the payment delivery, incase it is not delivered successfully.

                        return Task.FromResult(true);
                    }
                }
                else
                {
                    //ToDo: We may send email notification to the admin that we have received hook notification with different amount or different currency.
                    return Task.FromResult(false);
                }
            }
            else
            {
                //Hook notification for non existing checkout

                //ToDo: We may send email notification to the admin that we have received hook notification for non existing checkout.

                return Task.FromResult(false);
            }
        }

        public Task<bool> InvoiceHookNotification(DTOWebhookInfo dTOWebhookInfo)
        {
            DTOInvoiceInfo invoiceInfo = GetInvoiceInfo(long.Parse(dTOWebhookInfo.payload.customParameters.merchant_invoice_number)).Result;

            if (invoiceInfo.HyperPayMerchantInvoiceNumber != -1)
            {
                //Hook notification for existing invoice

                if (invoiceInfo.Amount == decimal.Parse(dTOWebhookInfo.payload.amount) &&
                    invoiceInfo.Currency == dTOWebhookInfo.payload.currency &&
                    invoiceInfo.PaymentType == dTOWebhookInfo.payload.paymentType)
                {

                    if (invoiceInfo.OwnerSystemDeliveryStatus == -1)
                    {
                        //No record in the payment delivery table.

                        string insertRes = InsertInvoicePaymentDelivery(long.Parse(dTOWebhookInfo.payload.customParameters.merchant_invoice_number)).Result;

                        if (insertRes.ToLower() == "success")
                        {
                            DTOInvoicePmtNotificationInfo dTOPmtNotificationInfo = new DTOInvoicePmtNotificationInfo
                            {
                                PaymentId = dTOWebhookInfo.payload.id,
                                OwnerSystemMerchantInvoiceNumber = invoiceInfo.OwnerSystemMerchantInvoiceNumber,
                                HyperPayMerchantInvoiceNumber = long.Parse(dTOWebhookInfo.payload.customParameters.merchant_invoice_number),
                                Amount = decimal.Parse(dTOWebhookInfo.payload.amount),
                                Currency = dTOWebhookInfo.payload.currency,
                                PaymentMethod = dTOWebhookInfo.payload.paymentBrand,
                                PaymentProcessingDate = DateTime.ParseExact(dTOWebhookInfo.payload.timestamp, "yyyy-MM-dd HH:mm:ss+0000", null).ToString("dd/MM/yyyy HH:mm:ss"),
                                SAPTCOCustomParameters = new SAPTCOCustomParameters
                                {
                                    CustomParameter1 = invoiceInfo.CustomParameter1,
                                    CustomParameter2 = invoiceInfo.CustomParameter2
                                }
                            };

                            if (!string.IsNullOrEmpty(invoiceInfo.HookNotificationTicketsUrl))
                                dTOPmtNotificationInfo.SAPTCOCustomParameters.TicketsURL = string.Format(invoiceInfo.HookNotificationTicketsUrl, invoiceInfo.OwnerSystemMerchantInvoiceNumber, invoiceInfo.Phone, invoiceInfo.Lang);


                            bool pmtNotificationProcessedSuccessfully = ProcessInvoicePmtNotificationToOwnerSystem(invoiceInfo.SystemId,
                                        invoiceInfo.OwnerSystemPmtNotificationURL, invoiceInfo.OwnerSystemPmtNotificationUserName,
                                        invoiceInfo.OwnerSystemPmtNotificationPassword, dTOPmtNotificationInfo).Result;

                            if (pmtNotificationProcessedSuccessfully && !string.IsNullOrEmpty(invoiceInfo.OwnerChannelHookNotificationURL))
                            {
                                if (invoiceInfo.OwnerChannelHookNotificationURL.Contains("{"))
                                    invoiceInfo.OwnerChannelHookNotificationURL = string.Format(invoiceInfo.OwnerChannelHookNotificationURL, invoiceInfo.CustomParameter1);

                                ProcessInvoicePmtNotificationToOwnerChannel(invoiceInfo.OwnerChannelHookNotificationURL, invoiceInfo.OwnerChannelHookNotificationUserName,
                                        invoiceInfo.OwnerChannelHookNotificationPassword, dTOPmtNotificationInfo);
                            }

                            return Task.FromResult(true);
                        }
                        else if (insertRes.ToLower() == "record_already_exist")
                        {
                            return Task.FromResult(true);
                        }
                        else
                        {
                            return Task.FromResult(false);
                        }
                    }
                    else
                    {
                        //As Long As there is a record in the payment delivery table, SAPTCO will return true to Hyperpay.
                        //It is the responsiblity of Hangfire retry job to retry the payment delivery, incase it is not delivered successfully.

                        return Task.FromResult(true);
                    }
                }
                else
                {
                    //ToDo: We may send email notification to the admin that we have received hook notification with different amount or different currency.
                    return Task.FromResult(false);
                }
            }
            else
            {
                //Hook notification for not existing invoice

                //ToDo: We may send email notification to the admin that we have received hook notification for non existing invoice.

                return Task.FromResult(false);
            }
        }

        public Task<bool> PaymentNotify(DTOPaymentNotifyRequest model)
        {
            try
            {
                model.httpBody = GetRequestBody();
                byte[] key = ToByteArray(model.WebhookDecryptionKey);
                byte[] iv = ToByteArray(model.ivFromHttpHeader);
                byte[] authTag = ToByteArray(model.authTagFromHttpHeader);
                byte[] encryptedText = ToByteArray(model.httpBody);
                byte[] cipherText = encryptedText.Concat(authTag).ToArray();

                // Prepare decryption
                GcmBlockCipher cipher = new GcmBlockCipher(new AesFastEngine());
                AeadParameters parameters = new AeadParameters(new KeyParameter(key), 128, iv);
                cipher.Init(false, parameters);

                // Decrypt
                var plainText = new byte[cipher.GetOutputSize(cipherText.Length)];
                var len = cipher.ProcessBytes(cipherText, 0, cipherText.Length, plainText, 0);
                cipher.DoFinal(plainText, len);

                string plainTextBody = Encoding.ASCII.GetString(plainText);

                DTOWebhookInfo dTOWebhookInfo = JsonConvert.DeserializeObject<DTOWebhookInfo>(plainTextBody);

                #region Insert Webhook
                long webhookRecordId = -1;
                using (var ctx = new HyperPayContext())
                {
                    string query = $"EXECUTE SP_InsertWebhook '{plainTextBody.Replace("'", "''")}','{dTOWebhookInfo.payload.ndc}','{dTOWebhookInfo.payload.merchantTransactionId}' ," +
                        $"'{dTOWebhookInfo.payload.merchantInvoiceId}', '{dTOWebhookInfo.payload.customParameters.merchant_invoice_number}', '{dTOWebhookInfo.type}', '{dTOWebhookInfo.payload.paymentType}'," +
                        $"'{dTOWebhookInfo.payload.paymentBrand}', '{dTOWebhookInfo.payload.amount}','{dTOWebhookInfo.payload.currency}','{dTOWebhookInfo.payload.result.code}' ," +
                        $"'{dTOWebhookInfo.payload.result.description.Replace("'", "''")}', '{dTOWebhookInfo.payload.card.bin}', '{dTOWebhookInfo.payload.card.last4Digits}' ," +
                        $"'{dTOWebhookInfo.payload.card.holder}', '{dTOWebhookInfo.payload.card.expiryMonth}' ," +
                        $"'{dTOWebhookInfo.payload.card.expiryYear}', '{dTOWebhookInfo.payload.customer.givenName}', '{dTOWebhookInfo.payload.customer.surname}' ," +
                        $"'{dTOWebhookInfo.payload.customer.email}', '{dTOWebhookInfo.payload.customer.ip}', '{dTOWebhookInfo.payload.billing?.street1}' ," +
                        $"'{dTOWebhookInfo.payload.billing?.city}', '{dTOWebhookInfo.payload.billing?.state}', '{dTOWebhookInfo.payload.billing?.postcode}' ," +
                        $"'{dTOWebhookInfo.payload.billing?.country}', '{dTOWebhookInfo.payload.authentication.entityId}', '{dTOWebhookInfo.payload.merchantAccountId}'," +
                        $"'{dTOWebhookInfo.payload.id}'";

                    webhookRecordId = (long)ctx.Database.SqlQuery<decimal>(query).FirstOrDefault();
                }
                #endregion

                if (webhookRecordId > 0)
                {

                    if (!string.IsNullOrEmpty(dTOWebhookInfo.payload.merchantInvoiceId))
                    {
                        //Hook for Invoice
                        if (IsInvoiceWebhookContainSuccessfulPayment(dTOWebhookInfo).Result)
                            return InvoiceHookNotification(dTOWebhookInfo);
                        else
                            //return true, so Hyperpay will consider that we processed the hook notification successfully.
                            return Task.FromResult(true);
                    }
                    else
                    {
                        //Hook for Checkout
                        if (IsCheckoutWebhookContainSuccessfulPayment(dTOWebhookInfo).Result)
                            return CheckoutHookNotification(dTOWebhookInfo);
                        else
                            //return true, so Hyperpay will consider that we processed the hook notification successfully.
                            return Task.FromResult(true);
                    }
                }
                else
                {
                    return Task.FromResult(false);
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(false);
            }
        }

        public Task<string> InsertInvoicePaymentDelivery(long hyperPayMerchantInvoiceNumber)
        {
            #region Insert Invoice Payment Delivery
            string insertRes;

            using (var ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_InsertInvoicePaymentDelivery {hyperPayMerchantInvoiceNumber}";

                insertRes = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
            }
            #endregion

            return Task.FromResult(insertRes);
        }

        public Task<string> InsertCheckoutPaymentDelivery(string checkoutId)
        {
            #region Insert Checkout Payment Delivery
            string insertRes;

            using (var ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_InsertCheckoutPaymentDelivery '{checkoutId}'";

                insertRes = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
            }
            #endregion

            return Task.FromResult(insertRes);
        }

        public Task<bool> IsCheckoutWebhookContainSuccessfulPayment(DTOWebhookInfo dTOWebhookRequest)
        {
            Match match = Regex.Match(dTOWebhookRequest.payload.result.code, @"^(000\.000\.|000\.100\.1|000\.[36])");

            if (dTOWebhookRequest.type == "PAYMENT" && match.Success &&
                dTOWebhookRequest.payload.paymentType == "DB" && decimal.Parse(dTOWebhookRequest.payload.amount) > 0)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> IsInvoiceWebhookContainSuccessfulPayment(DTOWebhookInfo dTOWebhookRequest)
        {
            Match match = Regex.Match(dTOWebhookRequest.payload.result.code, @"^(000\.000\.|000\.100\.1|000\.[36])");

            if (dTOWebhookRequest.type == "PAYMENT" && match.Success &&
                dTOWebhookRequest.payload.paymentType == "DB" && decimal.Parse(dTOWebhookRequest.payload.amount) > 0)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public Task<DTOCheckoutInfo> GetCheckoutInfo(string checkoutId)
        {
            DTOCheckoutInfo checkoutInfo = null;

            using (var ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_GetCheckoutInfo '{checkoutId}'";

                checkoutInfo = ctx.Database.SqlQuery<DTOCheckoutInfo>(query).FirstOrDefault();
            }

            return Task.FromResult(checkoutInfo);
        }

        public Task<DTOInvoiceInfo> GetInvoiceInfo(long HyperPayMerchantInvoiceNumber)
        {
            DTOInvoiceInfo invoiceInfo = null;

            using (var ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_GetInvoiceInfo '{HyperPayMerchantInvoiceNumber}'";

                invoiceInfo = ctx.Database.SqlQuery<DTOInvoiceInfo>(query).FirstOrDefault();
            }

            return Task.FromResult(invoiceInfo);
        }

        public Task<int> GetCheckoutPaymentDelivery(string checkoutId)
        {
            int PmtDeliveryStatusId;

            using (var ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_GetCheckoutPaymentDelivery '{checkoutId}'";

                PmtDeliveryStatusId = ctx.Database.SqlQuery<int>(query).FirstOrDefault();
            }

            return Task.FromResult(PmtDeliveryStatusId);
        }

        public Task<int> GetInvoicePaymentDelivery(long HyperPayMerchantInvoiceNumber)
        {
            int PmtDeliveryStatusId;

            using (var ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_GetInvoicePaymentDelivery '{HyperPayMerchantInvoiceNumber}'";

                PmtDeliveryStatusId = ctx.Database.SqlQuery<int>(query).FirstOrDefault();
            }

            return Task.FromResult(PmtDeliveryStatusId);
        }

        public Task<long> GetNextHyperPayInvoiceNumber()
        {
            long nextInvoiceNumber;

            using (var ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_GetNextHyperPayInvoiceNumber";

                nextInvoiceNumber = ctx.Database.SqlQuery<long>(query).FirstOrDefault();
            }

            return Task.FromResult(nextInvoiceNumber);
        }

        public Task<bool> IsInvoiceExist(long invoiceNumber, int systemId)
        {
            string isExist;

            using (var ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_IsInvoiceExist {invoiceNumber}, {systemId}";

                isExist = ctx.Database.SqlQuery<string>(query).FirstOrDefault();
            }

            if (isExist.ToLower() == "true")
                return Task.FromResult(true);
            else
                return Task.FromResult(false);
        }

        public Task<DTOPaymentStatusInfo> PaymentStatus(DTOPaymentStatus model)
        {
            var client = new RestClient(string.Format(ConfigurationManager.AppSettings["oppwaGetStatusUrl"], model.CheckoutId));
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", ConfigurationManager.AppSettings["bearerToken"]);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("entityId", FindEntityIdFromPaymentMethod(model.PaymentMethod).Result);
            IRestResponse response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<DTOPaymentStatusInfo>(response.Content);

            return Task.FromResult(result);
        }

        public Task<DTOInvoiceSuccessResponse> SuccessSimpleInvoiceResponse(string response)
        {
            var result = JsonConvert.DeserializeObject<DTOInvoiceSuccessResponse>(response);

            string shortURL = CreateShortURL(result.url).Result;

            if (!string.IsNullOrEmpty(shortURL))
                result.url = shortURL;

            return Task.FromResult(result);
        }

        public Task<string> CreateShortURL(string fullURL)
        {
            try
            {
                object _shortURLRq = new
                {
                    FullURL = fullURL
                };

                //specify to use TLS 1.2 as default connection
                //System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                var client = new RestClient(string.Format(ConfigurationManager.AppSettings["ShortURLBaseURL"] + "CreateShortURL"));
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", ConfigurationManager.AppSettings["bearerToken"]);
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", JsonConvert.SerializeObject(_shortURLRq), ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                var result = JsonConvert.DeserializeObject<ShortURLRs>(response.Content);

                return Task.FromResult(result.Response);
            }
            catch (Exception)
            {
                return Task.FromResult(string.Empty);
            }
        }

        static byte[] ToByteArray(string HexString)
        {
            int NumberChars = HexString.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(HexString.Substring(i, 2), 16);
            }
            return bytes;
        }

        public static string GetRequestBody()
        {
            var bodyStream = new StreamReader(HttpContext.Current.Request.InputStream);
            bodyStream.BaseStream.Seek(0, SeekOrigin.Begin);
            var bodyText = bodyStream.ReadToEnd();
            return bodyText;
        }

        public Task<DTOTRPaidResult> TRPaid(DTOTRPaid model)
        {
            string ConnectionString = ConfigurationManager.ConnectionStrings["TRConnectionString"].ConnectionString;
            DTOTRPaidResult response = new DTOTRPaidResult();

            try
            {
                using (OracleConnection cn = new OracleConnection(ConnectionString))
                {
                    OracleCommand cmd = new OracleCommand("ONLINE_WS_PKG.WS_PAY_TICKETS", cn);
                    cn.Open();
                    cmd.InitialLONGFetchSize = 1000;
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("p_channel", OracleDbType.Varchar2).Value = DBNull.Value;
                    cmd.Parameters.Add("p_usr_id", OracleDbType.Int32).Value = DBNull.Value;
                    cmd.Parameters.Add("p_lang_id", OracleDbType.Varchar2).Value = DBNull.Value;
                    cmd.Parameters.Add("p_sdp_id", OracleDbType.Int32).Value = DBNull.Value;
                    cmd.Parameters.Add("p_saptco_pnr", OracleDbType.Varchar2).Value = model.p_saptco_pnr;
                    cmd.Parameters.Add("p_payment_method_id", OracleDbType.Int32).Value = model.p_payment_method_id;
                    cmd.Parameters.Add("p_total_paid_amount", OracleDbType.Int32).Value = model.p_total_paid_amount;
                    cmd.Parameters.Add("p_transaction_id", OracleDbType.Varchar2).Value = model.p_transaction_id;
                    cmd.Parameters.Add("p_payment_process_date", OracleDbType.Varchar2).Value = model.p_payment_process_date;
                    cmd.Parameters.Add("p_is_paying", OracleDbType.Int32).Value = model.p_is_paying;
                    cmd.Parameters.Add("p_expiry_date", OracleDbType.Varchar2).Value = DBNull.Value;

                    cmd.Parameters.Add("p_is_paid", OracleDbType.Int32).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("p_excp_ticket_numbers", OracleDbType.Varchar2, 200).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("p_rtrn_cd", OracleDbType.Varchar2, 200).Direction = ParameterDirection.Output;
                    cmd.Parameters.Add("p_rtrn_desc", OracleDbType.Varchar2, 500).Direction = ParameterDirection.Output;

                    cmd.ExecuteNonQuery();

                    response.p_is_paid = cmd.Parameters["p_is_paid"].Value.ToString();
                    response.p_excp_ticket_numbers = cmd.Parameters["p_excp_ticket_numbers"].Value.ToString();
                    response.p_rtrn_cd = cmd.Parameters["p_rtrn_cd"].Value.ToString();
                    response.p_rtrn_desc = cmd.Parameters["p_rtrn_desc"].Value.ToString();

                    cn.Close();
                };
            }
            catch (Exception ex)
            {
                response = new DTOTRPaidResult
                {
                    p_rtrn_cd = "Exception",
                    p_rtrn_desc = ex.Message + " " + ex.InnerException?.Message + " " + ex.InnerException?.InnerException?.Message
                };
            }

            return Task.FromResult(response);
        }

        public Task<DTORefundResponse> ReBillPayment(DTOReBillPayment model)
        {
            string entityId = FindEntityIdFromPaymentMethod(model.PaymentMethod).Result;
            var client = new RestClient($"{ConfigurationManager.AppSettings["oppwaRefundUrl"]}/{model.PaymentId}");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", ConfigurationManager.AppSettings["bearerToken"]);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("entityId", entityId);
            request.AddParameter("amount", model.Amount);
            request.AddParameter("currency", model.Currency);
            request.AddParameter("paymentType", model.PaymentType);
            IRestResponse response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<DTORefundResponse>(response.Content);
            return Task.FromResult(result);
        }

        public Task<GlobalResponseMessage> VacationsInformation(DTOEmployeeVacationRequest model)
        {
            string ConnectionString = ConfigurationManager.ConnectionStrings["ERP.ERPRD.Oracle.ConnectionString"].ConnectionString;
            OracleManager DBManager = new OracleManager(ConnectionString);
            DataTable ReturnTable = DBManager.ExecuteCursorStoredProcedure("APPS.XX_HR_INTEGRATION.WS_VACATION_INFO", SetupVacationParamters(model.EmployeeID, null, null, null, model.FromDate, model.ToDate), true);
            if (ReturnTable == null || ReturnTable.Rows.Count < 1)
            {
                return Task.FromResult(new GlobalResponseMessage { Code = 0, Message = "No Vacation found" });
            }

            List<DTOEmployeeVacationResponse> ReturnList = new List<DTOEmployeeVacationResponse>();

            foreach (DataRow item in ReturnTable.Rows)
            {
                ReturnList.Add(ParseVacationInformation(item));

            }
            return Task.FromResult(new GlobalResponseMessage { Code = 1, Message = JsonConvert.SerializeObject(ReturnList) });
        }

        public Task<GlobalResponseMessage> ViolationInformation(DTOViolationRequest model)
        {

            string ConnectionString = ConfigurationManager.ConnectionStrings["ERP.ERPRD.Oracle.ConnectionString"].ConnectionString;
            OracleManager DBManager = new OracleManager(ConnectionString);
            DataTable ReturnTable = DBManager.ExecuteCursorStoredProcedure("APPS.XX_HR_INTEGRATION.WS_VIOLATION_CHAT", SetupViolationParamters(model.EmployeeID, model.FromDate, model.ToDate), true);
            if (ReturnTable == null || ReturnTable.Rows.Count < 1)
            {
                return Task.FromResult(new GlobalResponseMessage { Code = 0, Message = "No Violation found" });
            }

            List<DTOViolationResponse> ReturnList = new List<DTOViolationResponse>();

            foreach (DataRow item in ReturnTable.Rows)
            {
                ReturnList.Add(ParseViolationInformation(item));
            }
            return Task.FromResult(new GlobalResponseMessage { Code = 1, Message = JsonConvert.SerializeObject(ReturnList) });
        }

        public Task<DTOPaySlipResInfo> PaySlipInformation(DTOPaySlipReqInfo model)
        {
            try
            {
                string ConnectionString = ConfigurationManager.ConnectionStrings["ERP.ERPRD.Oracle.ConnectionString"].ConnectionString;
                var dbManager = new OracleManager(ConnectionString);
                var outputs = dbManager.ExecuteStoredProcedureAndReturnValues("APPS.XX_HR_INTEGRATION.WS_PAYSLIPS",
                    SetupPaySlipParamters(model));
                if (outputs == null) return null;
                var report = outputs["P_PAYSLIP_INFO"].DeserializeXml<G_REPORT>();
                var deductionElementsList = new HashSet<DeductionElement>();
                var earningElementList = new HashSet<EarningElement>();
                report.G_REGION.G_SECTOR.G_DEPT.EMPLOYEE.DEDUCTION_ELEMENT.ForEach(item => { deductionElementsList.Add(new DeductionElement() { Value = item.ELEMENT_VALUE, Name = item.ELEMENT_NAME }); });
                report.G_REGION.G_SECTOR.G_DEPT.EMPLOYEE.EARNING_ELEMENT.ForEach(item => { earningElementList.Add(new EarningElement() { Value = item.ELEMENT_VALUE, Name = item.ELEMENT_NAME }); });
                var response = new DTOPaySlipResInfo() { DepartmentName = report.G_REGION.G_SECTOR.G_DEPT.DEPT_NAME, RegionName = report.G_REGION.REG_NAME, SectorName = report.G_REGION.G_SECTOR.SECT_NAME, Employee = new Employee() { EmployeeName = report.G_REGION.G_SECTOR.G_DEPT.EMPLOYEE.EMPLOYEE_NAME, EmployeeNumber = report.G_REGION.G_SECTOR.G_DEPT.EMPLOYEE.EMPLOYEE_NUMBER, Grade = report.G_REGION.G_SECTOR.G_DEPT.EMPLOYEE.GRADE, HireDate = report.G_REGION.G_SECTOR.G_DEPT.EMPLOYEE.HIRE_DATE, LocationName = report.G_REGION.G_SECTOR.G_DEPT.EMPLOYEE.LOCATION_NAME, Nationality = report.G_REGION.G_SECTOR.G_DEPT.EMPLOYEE.NATIONALITY, NetValue = report.G_REGION.G_SECTOR.G_DEPT.EMPLOYEE.NET_VALUE, OrganizationName = report.G_REGION.G_SECTOR.G_DEPT.EMPLOYEE.ORG_NAME, PeriodName = report.G_REGION.G_SECTOR.G_DEPT.EMPLOYEE.PERIOD_NAME, PositionName = report.G_REGION.G_SECTOR.G_DEPT.EMPLOYEE.POSITION_NAME, TerminationDate = report.G_REGION.G_SECTOR.G_DEPT.EMPLOYEE.TERMINATION_DATE, TotalDeductions = report.G_REGION.G_SECTOR.G_DEPT.EMPLOYEE.TOT_DEDUCTIONS, TotalEarnings = report.G_REGION.G_SECTOR.G_DEPT.EMPLOYEE.TOT_EARNINGS, DeductionElements = deductionElementsList, EarningElements = earningElementList } };
                return Task.FromResult(response);
            }
            catch (Exception currentException)
            {
                throw currentException;
            }
        }

        public Task<DTOTicketBalanceRes> TicketBalance(DTOTicketBalanceReq model)
        {
            try
            {
                string ConnectionString = ConfigurationManager.ConnectionStrings["ERP.ERPRD.Oracle.ConnectionString"].ConnectionString;
                var dbManager = new OracleManager(ConnectionString);
                DataTable ReturnTable = dbManager.ExecuteCursorStoredProcedure("APPS.XX_HR_INTEGRATION.GET_TICKET_BALANCE",
                    TicketBalanceParamters(model), true);
                if (ReturnTable == null || ReturnTable.Rows.Count < 1)
                {
                    throw new NullReferenceException("No Tickets found");
                }

                DTOTicketBalanceRes ReturnObj = new DTOTicketBalanceRes();

                foreach (DataRow item in ReturnTable.Rows)
                {
                    ReturnObj.EMP_NO = item["EMP_NO"].ToString();
                    ReturnObj.EMP_NAME = item["EMP_NAME"].ToString();
                    ReturnObj.ticket_balance = Convert.ToDouble(item["ticket_balance"]);

                }
                return Task.FromResult(ReturnObj);
            }
            catch (Exception currentException)
            {
                throw currentException;
            }
        }


        #region Create Stored Parameters For Vacation

        private List<ServiceParameter> SetupVacationParamters(int? EmployeeID, DateTime? LastUpdatedDate, string SectorID, string EmployeeCategory, string FromDate, string ToDate)
        {
            List<ServiceParameter> parameters = new List<ServiceParameter>();

            #region P_LANG
            ServiceParameter Language = new ServiceParameter()
            {
                Name = "P_LANG",
                DataType = OracleDbType.NVarchar2,
                Direction = ParameterDirection.Input,
                Value = "AR"
            };

            parameters.Add(Language);
            #endregion 

            #region P_LSTDATE
            if (LastUpdatedDate.HasValue)
            {
                ServiceParameter P_LSTDATE = new ServiceParameter()
                {
                    Name = "P_LSTDATE",
                    DataType = OracleDbType.NVarchar2,
                    Direction = ParameterDirection.Input,
                    Value = LastUpdatedDate.Value.ToString("dd/MM/yyy")
                };

                parameters.Add(P_LSTDATE);
            }
            else
            {
                ServiceParameter P_LSTDATE = new ServiceParameter()
                {
                    Name = "P_LSTDATE",
                    DataType = OracleDbType.NVarchar2,
                    Direction = ParameterDirection.Input,
                    Value = (new DateTime(2014, 1, 1)).ToString("dd/MM/yyy")
                };

                parameters.Add(P_LSTDATE);
            }

            #endregion

            #region P_SECTOR_ID
            if (!string.IsNullOrEmpty(SectorID))
            {
                ServiceParameter P_SECTOR_ID = new ServiceParameter()
                {
                    Name = "P_SECTOR_ID",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = SectorID
                };

                parameters.Add(P_SECTOR_ID);
            }
            else
            {
                ServiceParameter P_SECTOR_ID = new ServiceParameter()
                {
                    Name = "P_SECTOR_ID",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = null
                };

                parameters.Add(P_SECTOR_ID);
            }
            #endregion

            #region P_EMPNO
            if (EmployeeID.HasValue)
            {
                ServiceParameter P_EMPNO = new ServiceParameter()
                {
                    Name = "P_EMPNO",
                    DataType = OracleDbType.Int64,
                    Direction = ParameterDirection.Input,
                    Value = EmployeeID.Value
                };

                parameters.Add(P_EMPNO);
            }
            else
            {
                ServiceParameter P_EMPNO = new ServiceParameter()
                {
                    Name = "P_EMPNO",
                    DataType = OracleDbType.Int64,
                    Direction = ParameterDirection.Input,
                    Value = null
                };

                parameters.Add(P_EMPNO);
            }
            #endregion

            #region P_EMP_CATEGORY
            if (EmployeeCategory != "")
            {
                ServiceParameter P_EMP_CATEGORY = new ServiceParameter()
                {
                    Name = "P_EMP_CATEGORY",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = EmployeeCategory
                };

                parameters.Add(P_EMP_CATEGORY);
            }
            else
            {
                ServiceParameter P_EMP_CATEGORY = new ServiceParameter()
                {
                    Name = "P_EMP_CATEGORY",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = null
                };

                parameters.Add(P_EMP_CATEGORY);
            }
            #endregion

            #region P_FROMDATE

            DateTime fromDT = DateTime.ParseExact(FromDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            ServiceParameter P_FROMDATE = new ServiceParameter()
            {
                Name = "P_FROMDATE",
                DataType = OracleDbType.NVarchar2,
                Direction = ParameterDirection.Input,
                Value = fromDT.ToString("dd/MM/yyy")
            };

            parameters.Add(P_FROMDATE);

            #endregion

            #region P_TODATE

            DateTime toDT = DateTime.ParseExact(ToDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            ServiceParameter P_TODATE = new ServiceParameter()
            {
                Name = "P_TODATE",
                DataType = OracleDbType.NVarchar2,
                Direction = ParameterDirection.Input,
                Value = toDT.ToString("dd/MM/yyy")
            };

            parameters.Add(P_TODATE);

            #endregion

            #region p_PROJECT_CODE
            ServiceParameter p_PROJECT_CODE = new ServiceParameter()
            {
                Name = "p_PROJECT_CODE",
                DataType = OracleDbType.Varchar2,
                Direction = ParameterDirection.Input,
                Value = null
            };

            parameters.Add(p_PROJECT_CODE);
            #endregion

            #region P_VAC_INFO
            //Output Parameters
            ServiceParameter EMPInfo = new ServiceParameter()
            {
                Name = "P_VAC_INFO",
                DataType = OracleDbType.RefCursor,
                Direction = ParameterDirection.Output
            };
            #endregion


            parameters.Add(EMPInfo);

            return parameters;
        }

        private DTOEmployeeVacationResponse ParseVacationInformation(DataRow CurrentInformation)
        {
            if (CurrentInformation == null)
                throw new NullReferenceException("Vacation information data row is null");

            try
            {
                DTOEmployeeVacationResponse ReturnValue = new DTOEmployeeVacationResponse();


                ReturnValue.ABSENCE_DAYS = CurrentInformation["ABSENCE_DAYS"].ToString();
                ReturnValue.EMP_CATEGORY = CurrentInformation["EMP_CATEGORY"].ToString();
                ReturnValue.EMP_CATID = CurrentInformation["EMP_CATID"].ToString();
                ReturnValue.EMP_NAME = CurrentInformation["EMP_NAME"].ToString();
                ReturnValue.EMP_NUMBER = CurrentInformation["EMP_NUMBER"].ToString();
                ReturnValue.END_DATE = CurrentInformation["END_DATE"].ToString();
                ReturnValue.ENG_NAME = CurrentInformation["ENG_NAME"].ToString();
                ReturnValue.POSITION = CurrentInformation["POSITION"].ToString();
                ReturnValue.START_DATE = CurrentInformation["START_DATE"].ToString();
                ReturnValue.VACATION_ID = CurrentInformation["VACATION_ID"].ToString();
                ReturnValue.VACATION_NAME = CurrentInformation["VACATION_NAME"].ToString();

                if (ReturnValue.START_DATE != "")
                {
                    DateTime StartDate = DateTime.Parse(ReturnValue.START_DATE);

                    ReturnValue.START_DATE = StartDate.ToString("dd/MM/yyy");
                }

                if (ReturnValue.END_DATE != "")
                {
                    DateTime EndDate = DateTime.Parse(ReturnValue.END_DATE);

                    ReturnValue.END_DATE = EndDate.ToString("dd/MM/yyy");
                }


                return ReturnValue;
            }
            catch (Exception EE)
            {
                throw EE;
            }

        }

        #endregion

        #region Create Stored Parameters For Violation

        private List<ServiceParameter> SetupViolationParamters(int? EmployeeID, string FromDate, string ToDate)
        {
            List<ServiceParameter> parameters = new List<ServiceParameter>();


            #region P_LANG
            ServiceParameter Language = new ServiceParameter()
            {
                Name = "P_LANG",
                DataType = OracleDbType.NVarchar2,
                Direction = ParameterDirection.Input,
                Value = "AR"
            };

            parameters.Add(Language);
            #endregion

            #region P_EMPNO
            if (EmployeeID.HasValue)
            {
                ServiceParameter P_EMPNO = new ServiceParameter()
                {
                    Name = "P_EMPNO",
                    DataType = OracleDbType.Int64,
                    Direction = ParameterDirection.Input,
                    Value = EmployeeID.Value
                };

                parameters.Add(P_EMPNO);
            }
            else
            {
                ServiceParameter P_EMPNO = new ServiceParameter()
                {
                    Name = "P_EMPNO",
                    DataType = OracleDbType.Int64,
                    Direction = ParameterDirection.Input,
                    Value = null
                };

                parameters.Add(P_EMPNO);
            }
            #endregion

            #region P_FROMDATE

            DateTime fromDT = DateTime.ParseExact(FromDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            ServiceParameter P_FROMDATE = new ServiceParameter()
            {
                Name = "P_FROMDATE",
                DataType = OracleDbType.NVarchar2,
                Direction = ParameterDirection.Input,
                Value = fromDT.ToString("dd/MM/yyy")
            };

            parameters.Add(P_FROMDATE);

            #endregion

            #region P_TODATE

            DateTime toDT = DateTime.ParseExact(ToDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            ServiceParameter P_TODATE = new ServiceParameter()
            {
                Name = "P_TODATE",
                DataType = OracleDbType.NVarchar2,
                Direction = ParameterDirection.Input,
                Value = toDT.ToString("dd/MM/yyy")
            };

            parameters.Add(P_TODATE);

            #endregion

            //Output Parameters
            ServiceParameter P_VIOL_INFO = new ServiceParameter()
            {
                Name = "P_VIOL_INFO",
                DataType = OracleDbType.RefCursor,
                Direction = ParameterDirection.Output
            };

            parameters.Add(P_VIOL_INFO);

            return parameters;
        }

        private DTOViolationResponse ParseViolationInformation(DataRow CurrentInformation)
        {
            if (CurrentInformation == null)
                throw new NullReferenceException("Violation information data row is null");

            try
            {
                DTOViolationResponse ReturnValue = new DTOViolationResponse();

                ReturnValue.DRIVER_NO = CurrentInformation["EMPNO"].ToString();
                ReturnValue.full_name = CurrentInformation["EMP_NAME"].ToString();
                ReturnValue.Eng_name = CurrentInformation["Eng_name"].ToString();
                ReturnValue.EMP_CATID = CurrentInformation["EMP_CATID"].ToString();
                ReturnValue.EMP_CATEGORY = CurrentInformation["EMP_CATEGORY"].ToString();
                ReturnValue.SECT_ID = CurrentInformation["SECT_ID"].ToString();
                ReturnValue.SECT_NAME = CurrentInformation["SECT_NAME"].ToString();
                ReturnValue.VIOLATION_TYPE = CurrentInformation["VIOLATION_TYPE"].ToString();
                ReturnValue.VIOLATION_DATE = CurrentInformation["VIOLATION_DATE"].ToString();
                ReturnValue.REPEAT_NO = CurrentInformation["REPEAT_NO"].ToString();
                ReturnValue.PENALTY_TYPE = CurrentInformation["PENALTY_TYPE"].ToString();
                ReturnValue.DEDUCTION_TYPE = CurrentInformation["DEDUCTION_TYPE"].ToString();
                ReturnValue.DEDUCTION_AMOUNT = CurrentInformation["DEDUCTION_AMOUNT"].ToString();

                DateTime VIOLATION_DATE = DateTime.ParseExact(ReturnValue.VIOLATION_DATE, "M/d/yyyy hh:mm:ss tt", null);

                ReturnValue.VIOLATION_DATE = VIOLATION_DATE.ToString("dd/MM/yyy");

                return ReturnValue;
            }
            catch (Exception EE)
            {
                throw EE;
            }

        }

        #endregion

        #region Create Stored Parameters For PaySlip

        private List<ServiceParameter> SetupPaySlipParamters(DTOPaySlipReqInfo payslipRqInfo)
        {
            List<ServiceParameter> parameters = new List<ServiceParameter>();

            #region p_errbuf
            ServiceParameter p_errbuf = new ServiceParameter()
            {
                Name = "p_errbuf",
                DataType = OracleDbType.Varchar2,
                Direction = ParameterDirection.Output,
                Size = 200
            };

            parameters.Add(p_errbuf);
            #endregion

            #region p_retcode           
            ServiceParameter p_retcode = new ServiceParameter()
            {
                Name = "p_retcode",
                DataType = OracleDbType.Int32,
                Direction = ParameterDirection.Output,
                Size = 200
            };

            parameters.Add(p_retcode);
            #endregion

            #region P_PERIOD_ID                  
            if (!string.IsNullOrEmpty(payslipRqInfo.PeriodId))
            {
                ServiceParameter P_PERIOD_ID = new ServiceParameter()
                {
                    Name = "P_PERIOD_ID",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = payslipRqInfo.PeriodId
                };

                parameters.Add(P_PERIOD_ID);
            }
            else
            {
                ServiceParameter P_PERIOD_ID = new ServiceParameter()
                {
                    Name = "P_PERIOD_ID",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = null
                };

                parameters.Add(P_PERIOD_ID);
            }
            #endregion

            #region P_FROMDATE

            DateTime fromDT = DateTime.ParseExact(payslipRqInfo.FromDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            ServiceParameter P_FROMDATE = new ServiceParameter()
            {
                Name = "P_FROMDATE",
                DataType = OracleDbType.NVarchar2,
                Direction = ParameterDirection.Input,
                Value = fromDT.ToString("dd/MM/yyy")
            };

            parameters.Add(P_FROMDATE);

            #endregion

            #region P_TODATE

            DateTime toDT = DateTime.ParseExact(payslipRqInfo.ToDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            ServiceParameter P_TODATE = new ServiceParameter()
            {
                Name = "P_TODATE",
                DataType = OracleDbType.NVarchar2,
                Direction = ParameterDirection.Input,
                Value = toDT.ToString("dd/MM/yyy")
            };

            parameters.Add(P_TODATE);

            #endregion

            #region P_PAYROLL_RUN_ID
            if (!string.IsNullOrEmpty(payslipRqInfo.PayrollRunId))
            {
                ServiceParameter P_PAYROLL_RUN_ID = new ServiceParameter()
                {
                    Name = "P_PAYROLL_RUN_ID",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = payslipRqInfo.PayrollRunId
                };

                parameters.Add(P_PAYROLL_RUN_ID);
            }
            else
            {
                ServiceParameter P_PAYROLL_RUN_ID = new ServiceParameter()
                {
                    Name = "P_PAYROLL_RUN_ID",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = null
                };

                parameters.Add(P_PAYROLL_RUN_ID);
            }
            #endregion

            #region P_REGION_FROM
            if (!string.IsNullOrEmpty(payslipRqInfo.RegionFrom))
            {
                ServiceParameter P_REGION_FROM = new ServiceParameter()
                {
                    Name = "P_REGION_FROM",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = payslipRqInfo.RegionFrom
                };

                parameters.Add(P_REGION_FROM);
            }
            else
            {
                ServiceParameter P_REGION_FROM = new ServiceParameter()
                {
                    Name = "P_REGION_FROM",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = null
                };

                parameters.Add(P_REGION_FROM);
            }
            #endregion

            #region P_REGION_TO
            if (!string.IsNullOrEmpty(payslipRqInfo.RegionTo))
            {
                ServiceParameter P_REGION_TO = new ServiceParameter()
                {
                    Name = "P_REGION_TO",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = payslipRqInfo.RegionTo
                };

                parameters.Add(P_REGION_TO);
            }
            else
            {
                ServiceParameter P_REGION_TO = new ServiceParameter()
                {
                    Name = "P_REGION_TO",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = null
                };

                parameters.Add(P_REGION_TO);
            }
            #endregion

            #region P_SECTOR_FROM 
            if (!string.IsNullOrEmpty(payslipRqInfo.SectorFrom))
            {
                ServiceParameter P_SECTOR_FROM = new ServiceParameter()
                {
                    Name = "P_SECTOR_FROM",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = payslipRqInfo.SectorFrom
                };

                parameters.Add(P_SECTOR_FROM);
            }
            else
            {
                ServiceParameter P_SECTOR_FROM = new ServiceParameter()
                {
                    Name = "P_SECTOR_FROM",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = null
                };

                parameters.Add(P_SECTOR_FROM);
            }
            #endregion

            #region P_SECTOR_TO
            if (!string.IsNullOrEmpty(payslipRqInfo.SectorTo))
            {
                ServiceParameter P_SECTOR_TO = new ServiceParameter()
                {
                    Name = "P_SECTOR_TO",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = payslipRqInfo.SectorTo
                };

                parameters.Add(P_SECTOR_TO);
            }
            else
            {
                ServiceParameter P_SECTOR_TO = new ServiceParameter()
                {
                    Name = "P_SECTOR_TO",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = null
                };

                parameters.Add(P_SECTOR_TO);
            }
            #endregion

            #region P_DEPT_FROM
            if (!string.IsNullOrEmpty(payslipRqInfo.DepartmentFrom))
            {
                ServiceParameter P_DEPT_FROM = new ServiceParameter()
                {
                    Name = "P_DEPT_FROM",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = payslipRqInfo.DepartmentFrom
                };

                parameters.Add(P_DEPT_FROM);
            }
            else
            {
                ServiceParameter P_DEPT_FROM = new ServiceParameter()
                {
                    Name = "P_DEPT_FROM",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = null
                };

                parameters.Add(P_DEPT_FROM);
            }
            #endregion

            #region P_DEPT_TO
            if (!string.IsNullOrEmpty(payslipRqInfo.DepartmentTo))
            {
                ServiceParameter P_DEPT_TO = new ServiceParameter()
                {
                    Name = "P_DEPT_TO",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = payslipRqInfo.DepartmentTo
                };

                parameters.Add(P_DEPT_TO);
            }
            else
            {
                ServiceParameter P_DEPT_TO = new ServiceParameter()
                {
                    Name = "P_DEPT_TO",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = null
                };

                parameters.Add(P_DEPT_TO);
            }
            #endregion

            #region p_empno
            if (!string.IsNullOrEmpty(payslipRqInfo.EmployeeId))
            {
                ServiceParameter p_empno = new ServiceParameter()
                {
                    Name = "p_empno",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = payslipRqInfo.EmployeeId
                };

                parameters.Add(p_empno);
            }
            else
            {
                ServiceParameter p_empno = new ServiceParameter()
                {
                    Name = "p_empno",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = null
                };

                parameters.Add(p_empno);
            }
            #endregion

            #region P_LANG
            ServiceParameter P_LANG = new ServiceParameter()
            {
                Name = "P_LANG",
                DataType = OracleDbType.Varchar2,
                Direction = ParameterDirection.Input,
                Value = payslipRqInfo.Lang
            };

            parameters.Add(P_LANG);
            #endregion

            #region p_payslip_info
            ServiceParameter p_payslip_info = new ServiceParameter()
            {
                Name = "p_payslip_info",
                DataType = OracleDbType.Varchar2,
                Direction = ParameterDirection.Output,
                Size = 100000
            };

            parameters.Add(p_payslip_info);
            #endregion

            return parameters;
        }

        #endregion

        #region  Create Stored Parameters For TicketBalance

        private List<ServiceParameter> TicketBalanceParamters(DTOTicketBalanceReq ticketBalanceInfo)
        {
            List<ServiceParameter> parameters = new List<ServiceParameter>();

            #region p_empno
            if (!string.IsNullOrEmpty(ticketBalanceInfo.EmployeeID))
            {
                ServiceParameter p_empno = new ServiceParameter()
                {
                    Name = "P_EMPNO",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = ticketBalanceInfo.EmployeeID
                };

                parameters.Add(p_empno);
            }
            else
            {
                ServiceParameter p_empno = new ServiceParameter()
                {
                    Name = "P_EMPNO",
                    DataType = OracleDbType.Varchar2,
                    Direction = ParameterDirection.Input,
                    Value = null
                };

                parameters.Add(p_empno);
            }
            #endregion


            #region P_REF_CURSOR
            ServiceParameter p_ref_cursor = new ServiceParameter()
            {
                Name = "P_REF_CURSOR",
                DataType = OracleDbType.RefCursor,
                Direction = ParameterDirection.Output
            };

            parameters.Add(p_ref_cursor);
            #endregion

            return parameters;
        }

        #endregion
    }
}
