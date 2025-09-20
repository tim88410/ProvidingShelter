using Microsoft.AspNetCore.Mvc;
using ProvidingShelter.Application.Services;
using ProvidingShelter.Common;

namespace ProvidingShelter.Controllers
{
    [ApiResult]
    [APIError]
    [ApiController]
    [Route("v1/sexual-assault")]
    public class SexualAssaultStatsController : ControllerBase
    {
        private readonly ISexualAssaultImportOrchestrator _orchestrator;

        public SexualAssaultStatsController(ISexualAssaultImportOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        /// <summary>
        /// 上傳「性侵害案件被害人國籍別交叉統計」ODS 檔，免費路線：ODS→XLSX→解析→入庫。
        /// </summary>
        /// /// <remarks>
        /// <code>
        /// <br/>
        /// 性侵害案件通報外籍被害人國籍別與行業別交叉統計 <br/>
        /// ParamError(2) 未收到檔案<br/>
        /// OperationFailed(4) 更新失敗<br/>
        /// OperationSuccessful(5) 更新成功<br/>
        /// </code>
        /// </remarks>
        [HttpPost("nationality/upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
            {
                throw new APIError.ParamError("未收到檔案。");
            }

            try
            {
                var importId = await _orchestrator.UploadAndImportAgeByNationalityAsync(file, ct);
                return Ok(importId);
            }
            catch (Exception ex)
            {
                throw new APIError.OperationFailed();
            }
        }
    }
}
