using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace ProvidingShelter.Common
{
    public class ApiResultAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null && !(context.Exception is APIError))
            {
                return;
            }

            if (context.Exception is APIError)
            {
                context.Result = new ObjectResult(((APIError)context.Exception).GetApiResult());
                return;
            }

            if (context.HttpContext.Response.StatusCode != StatusCodes.Status200OK)
            {
                return;
            }

            context.HttpContext.Request.Headers.TryGetValue("Compress-Output", out StringValues headerValue);
            _ = bool.TryParse(headerValue.ToString(), out bool needTocompress);

            if (context.Result is ApiResult.Result)
            {
                return;
            }

            var data = context.Result is not ObjectResult objectContent
                ? null
                : objectContent.Value;

            context.Result = new ObjectResult(new ApiResult.Result
            {
                ReturnCode = ErrorCode.ReturnCode.OperationSuccessful,
                ReturnData = data
            });
        }
    }
}
