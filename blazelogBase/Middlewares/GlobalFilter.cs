using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

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
        private readonly string appBasePath;
        public GlobalFilter(string basepath)
        {
             this.appBasePath   = basepath;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            
            var itcontroller = context.Controller as Controller;
            itcontroller!.ViewData[CN.Setting.AppBasePath] = this.appBasePath;
            // Do something before the action executes.
            await next();
            // Do something after the action executes.
        }
    }
}
