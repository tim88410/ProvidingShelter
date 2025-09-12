using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProvidingShelter.Application.Commands.Import;
using ProvidingShelter.Common;

namespace ProvidingShelter.Controllers
{
    /// <summary>
    /// 撈取 政府資料開放平台 103年至今的性侵害案件資料
    /// 目前資料集ID為147094 152223 174404
    /// FieldDesc LIKE '%OWNERCITYCODE%'
    /// 指令 importSexualAssault
    /// </summary>
    /// <remarks>
    /// <code>
    /// <br/>
    /// 指令 importSexualAssault <br/>
    /// OperationFailed(4) 更新失敗<br/>
    /// OperationSuccessful(5) 更新成功<br/>
    /// </code>
    /// </remarks>
    [ApiResult]
    [APIError]
    [ApiController]
    [Route("v1/Import")]
    public class ImportController : ControllerBase
    {
        private readonly IMediator mediator;
        //private readonly IImportCommandHandler _handler;

        public ImportController(IMediator mediator)
        {
            this.mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ImportSexualAssaultCommand command)
        {
            var res = await mediator.Send(command);
            if (res.ReturnCode != ErrorCode.ReturnCode.OperationSuccessful) //return BadRequest(new { message });
                throw new APIError.OperationFailed();

            return Ok();
        }
    }
}
