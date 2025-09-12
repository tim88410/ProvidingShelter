using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProvidingShelter.Common
{
    public class APIErrorAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            if (context.Exception != null && !(context.Exception is APIError))
            {
                return;
            }

            context.Result = new ObjectResult(((APIError)context.Exception).GetApiResult());
        }
    }
}
