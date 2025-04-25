using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace blazelogBase.Middlewares
{

    public class SampleAsyncActionFilter : IAsyncActionFilter
    {

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Do something before the action executes.
            await next();
            // Do something after the action executes.
        }
    }
    public class GlobalFilter:IAsyncActionFilter
    {
        private readonly string appBaseUrl;
        public GlobalFilter(IHttpContextAccessor accessor)
        {
            var request = accessor.HttpContext!.Request;
            this.appBaseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
            
             
        }
        
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            
            var itcontroller = context.Controller as Controller;
            itcontroller!.ViewData[CN.Setting.BaseUrl] = this.appBaseUrl;
            // Do something before the action executes.
            await next();
            // Do something after the action executes.
        }
    }
}
