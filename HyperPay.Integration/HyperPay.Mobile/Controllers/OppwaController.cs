using HyperPay.Service;
using HyperPay.Shared.Dtos;
using HyperPay.Shared.Models;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace HyperPay.Mobile.Controllers
{
    [Authorize]
    public class OppwaController : ApiController
    {
        readonly ISetupServer _setupServer;
        public OppwaController(ISetupServer setupServer)
        {
            this._setupServer = setupServer;
        }


        [HttpPost, Route("api/Checkouts")]
        public async Task<IHttpActionResult> Checkouts([FromBody] DTOMerchand model)
        {
            DTOUserMaster currentUser = JsonConvert.DeserializeObject<DTOUserMaster>(HttpContext.Current.User.Identity.Name);

            #region Validate Payment Method
            if (model.PaymentMethod != "VISA" && model.PaymentMethod != "MASTER" && model.PaymentMethod != "MADA")
            {
                DTOCheckOutResponse result = new DTOCheckOutResponse
                {
                    result = new Result
                    {
                        code = "Pmt_Method_NotValid",
                        description = "Payment method should be one of the following: VISA, MASTER, MADA"
                    }
                };

                return Ok(result);
            }
            #endregion

            #region Retreive Today's checkout statistics
            DTOCheckoutStatistics checkoutStatistics;

            using (var ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_GetCheckoutStatisticsByUser {currentUser.Id}";

                checkoutStatistics = ctx.Database.SqlQuery<DTOCheckoutStatistics>(query).FirstOrDefault();
            }
            #endregion

            if (checkoutStatistics.CheckoutCount + 1 > currentUser.DailyCheckoutAllowedCount ||
                checkoutStatistics.CheckoutSUM + (decimal)model.Amount > currentUser.DailyCheckoutAllowedAmount)
            {
                DTOCheckOutResponse result = new DTOCheckOutResponse
                {
                    result = new Result
                    {
                        code = "Daily_Limit_Exceeded",
                        description = "Allowed daily limit was exceeded."
                    }
                };

                return Ok(result);
            }
            else
            {
                var result = await this._setupServer.CheckoutsAsync(model, currentUser);

                return Ok(result);
            }
        }

        [HttpPost, Route("api/CreatePaymentForm")]
        public async Task<HttpResponseMessage> CreatePaymentForm([FromBody] DTOSumbitPayment model)
        {
            var response = new HttpResponseMessage();
            var result = await this._setupServer.CreatePaymentForm(model.CheckoutId, model.ReturnURL);
            response.Content = new StringContent(result);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return response;
        }

        [HttpPost, Route("api/PaymentStatus")]
        public async Task<IHttpActionResult> PaymentStatus([FromBody] DTOPaymentStatus model)
        {
            DTOUserMaster currentUser = JsonConvert.DeserializeObject<DTOUserMaster>(HttpContext.Current.User.Identity.Name);

            var result = await this._setupServer.PaymentStatus(model);

            Match match = Regex.Match(result.result.code, @"^(000\.000\.|000\.100\.1|000\.[36])");

            if (match.Success)
            {
                bool processedSuccessfully = await this._setupServer.ProcessPmtNotification(model.CheckoutId, result);

                if (processedSuccessfully)
                {
                    result.SAPTCOResult = new Result
                    {
                        code = "paid_delivered",
                        description = "paid and delivered successfully to the owner system"
                    };
                }
                else
                {
                    result.SAPTCOResult = new Result
                    {
                        code = "paid_notDelivered",
                        description = "paid but still not delivered to the owner system"
                    };
                }
            }
            else
            {
                result.SAPTCOResult = new Result
                {
                    code = "notPaid",
                    description = "not paid"
                };
            }

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost, Route("api/PaymentNotify")]
        public async Task<IHttpActionResult> PaymentNotify()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;
                DTOPaymentNotifyRequest model = new DTOPaymentNotifyRequest
                {
                    ivFromHttpHeader = httpRequest.Headers["X-Initialization-Vector"],
                    authTagFromHttpHeader = httpRequest.Headers["X-Authentication-Tag"]
                };

                var success = await this._setupServer.PaymentNotify(model);

                if (success)
                {
                    return Ok();
                }
                else
                {
                    return InternalServerError();
                }
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        [HttpPost, Route("api/SimpleInvoice")]
        public async Task<IHttpActionResult> SimpleInvoice([FromBody] DTOSimpleInvoice model)
        {
            DTOUserMaster currentUser = JsonConvert.DeserializeObject<DTOUserMaster>(HttpContext.Current.User.Identity.Name);

            #region Validate Daily Limit
            DTOInvoiceStatistics invoiceStatistics;

            using (var ctx = new HyperPayContext())
            {
                string query = $"EXECUTE SP_GetInvoiceStatisticsByUser {currentUser.Id}";

                invoiceStatistics = ctx.Database.SqlQuery<DTOInvoiceStatistics>(query).FirstOrDefault();
            }

            if (invoiceStatistics.InvoiceCount + 1 > currentUser.DailyInvoicesAllowedCount ||
                invoiceStatistics.InvoiceSUM + decimal.Parse(model.amount) > currentUser.DailyInvoicesAllowedAmount)
            {
                DTOInvoiceErrorResponse limitExceededResult = new DTOInvoiceErrorResponse
                {
                    status = false,
                    message = "SAPTCO: Allowed daily limit was exceeded."
                };

                return Ok(limitExceededResult);
            }
            #endregion

            #region Check if the invoice number was generated previously by the requester's system

            bool isExist = this._setupServer.IsInvoiceExist(long.Parse(model.merchant_invoice_number), currentUser.SystemId).Result;

            if (isExist)
            {
                DTOInvoiceErrorResponse invoiceExist = new DTOInvoiceErrorResponse
                {
                    status = false,
                    message = "SAPTCO: The merchant invoice number has already been taken.",
                    errors = new DTOErrors
                    {
                        merchant_invoice_number = new System.Collections.Generic.List<string>
                        {
                            "SAPTCO: The merchant invoice number has already been taken."
                        }
                    }
                };

                return Ok(invoiceExist);
            }
            #endregion

            var result = await this._setupServer.GenerateSimpleInvoice(model, currentUser);
            var invoiceStatus = JsonConvert.DeserializeObject<DTOInvoiceStatus>(result).status;

            if (invoiceStatus)
            {
                var success = await this._setupServer.SuccessSimpleInvoiceResponse(result);
                return Ok(success);
            }
            var failure = await this._setupServer.ErrorSimpleInvoiceResponse(result);
            return Ok(failure);
        }

        [AllowAnonymous]
        [HttpPost, Route("api/ReBillPayment")]
        public async Task<IHttpActionResult> ReBillPayment([FromBody] DTOReBillPayment model)
        {
            var result = await this._setupServer.ReBillPayment(model);
            return Ok(result);
        }
    }
}
