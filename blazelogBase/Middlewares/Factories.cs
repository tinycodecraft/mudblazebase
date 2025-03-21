using blazelogBase.Models;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Resources;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using blazelogBase.Resources;

namespace blazelogBase.Middlewares;


public  class ViewModelFactory
{
    public static ChangeLangModel CreateChangeLangModel()
    {
        var model =  new ChangeLangModel();
        model.ListOfLanguages = new List<SelectListItem>
        {
            new SelectListItem("English", "en-US"),
            new SelectListItem("中文", "zh-HK")
        };
        return model;
    }

    public static T CreateViewModelWithResource<T>(IStringLocalizer loc, string suffix = "FromLoc") where T : class, new()
    {
        //Please note that R is not the true name of the resource file
        //it is namespace for stringlocalizer to find the resource file
        var rmgr = new ResourceManager(typeof(SharedResource).FullName!, typeof(Program).Assembly);
        
        var model = new T();

       foreach(var p in typeof(T).GetProperties())
        {
            if(p.PropertyType == typeof(string) && p.Name.EndsWith(suffix))
            {
                
                p.SetValue(model, loc[p.Name].Value);
            }
            else 
            {
                try
                {
                    var text = rmgr.GetString(p.Name, Thread.CurrentThread.CurrentCulture);
                    if (text != null)
                    {
                        p.SetValue(model, text);
                    }

                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }



            }
        }

       return model;
    }
}

public class CustomProblemDetailsFactory : ProblemDetailsFactory
{

    public override ProblemDetails CreateProblemDetails(HttpContext httpContext, int? statusCode = null, string? title = null,
        string? type = null, string? detail = null, string? instance = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = instance,
        };

        return problemDetails;
    }

    public override ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext,
        ModelStateDictionary modelStateDictionary, int? statusCode = null, string? title = null, string? type = null,
        string? detail = null, string? instance = null)
    {
        statusCode ??= 400;
        type ??= "https://tools.ietf.org/html/rfc7231#section-6.5.1";
        instance ??= httpContext.Request.Path;

        var problemDetails = new CustomValidationProblemDetails(modelStateDictionary)
        {
            Status = statusCode,
            Type = type,
            Detail = detail,
            Instance = instance,
        };

        if (title != null)
        {
            // For validation problem details, don't overwrite the default title with null.
            problemDetails.Title = title;
        }

        var traceId = Activity.Current?.Id ?? httpContext?.TraceIdentifier;
        if (traceId != null)
        {
            problemDetails.Extensions["traceId"] = traceId;
        }

        return problemDetails;
    }
}