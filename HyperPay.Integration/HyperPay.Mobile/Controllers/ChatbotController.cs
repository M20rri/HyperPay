using HyperPay.Service;
using HyperPay.Shared.Dtos;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;


namespace HyperPay.Mobile.Controllers
{
    [Authorize]
    public class ChatbotController : ApiController
    {
        readonly ISetupServer _setupServer;

        public ChatbotController(ISetupServer setupServer)
        {
            this._setupServer = setupServer;
        }

        [HttpPost, Route("api/EmployeeVacations")]
        public async Task<IHttpActionResult> EmployeeVacations([FromBody] DTOEmployeeVacationRequest model)
        {
            if (!ModelState.IsValid)
            {
                string messages = string.Join("; ", ModelState.Values
                                           .SelectMany(x => x.Errors)
                                           .Select(x => x.ErrorMessage));

                return BadRequest(messages);

            }

            var result = await this._setupServer.VacationsInformation(model);
            if (result.Code == 0)
            {
                return Ok(result.Message);
            }
            return Ok(JsonConvert.DeserializeObject<List<DTOEmployeeVacationResponse>>(result.Message));
        }

        [HttpPost, Route("api/Violations")]
        public async Task<IHttpActionResult> Violations([FromBody] DTOViolationRequest model)
        {

            if (!ModelState.IsValid)
            {
                string messages = string.Join("; ", ModelState.Values
                                           .SelectMany(x => x.Errors)
                                           .Select(x => x.ErrorMessage));

                return BadRequest(messages);

            }

            var result = await this._setupServer.ViolationInformation(model);
            if (result.Code == 0)
            {
                return Ok(result.Message);
            }
            return Ok(JsonConvert.DeserializeObject<List<DTOViolationResponse>>(result.Message));
        }       
        
        [HttpPost, Route("api/PaySlip")]
        public async Task<IHttpActionResult> PaySlip([FromBody] DTOPaySlipReqInfo model)
        {
            if (!ModelState.IsValid)
            {
                string messages = string.Join("; ", ModelState.Values
                                           .SelectMany(x => x.Errors)
                                           .Select(x => x.ErrorMessage));

                return BadRequest(messages);

            }

            var result = await this._setupServer.PaySlipInformation(model);
            return Ok(result);
        }

        [HttpPost, Route("api/TicketBalance")]
        public async Task<IHttpActionResult> TicketBalance([FromBody] DTOTicketBalanceReq model)
        {
            var result = await this._setupServer.TicketBalance(model);
            return Ok(result);
        }
    }
}
