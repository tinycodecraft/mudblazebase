using blazelogBase.Components;
using blazelogBase.Control;
using blazelogBase.Middlewares;
using blazelogBase.Models;
using blazelogBase.Resources;
using blazelogBase.Shared.Tools;
using blazelogBase.Store.Setup;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Routing;
using MudBlazor;
using MudBlazor.Services;
using Serilog;
using System.Text.Json.Serialization;


/*
 * Please study 
 * https://www.fluentui-blazor.net/
 * https://github.com/microsoft/fluentui-blazor
 * for using blazor easily
 */

/*Bootstrap logger
 */
Log.Logger = new LoggerConfiguration().MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ApplicationName = typeof(Program).Assembly.GetName().Name,
    ContentRootPath = Directory.GetCurrentDirectory(),
    //WebRootPath = "wwwroot",

});

var baseurl = builder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey);

//Setting configurations
var authsetting = builder.Configuration.GetSection(CN.Setting.AuthSetting);
builder.Services.Configure<AuthSetting>(authsetting);



builder.Services.Configure<FormOptions>(opt =>
{

    opt.BufferBodyLengthLimit = 512 * 1024 * 1024;

    //it needs
    opt.MultipartBodyLengthLimit = 512 * 1024 * 1024;

});

builder.Services.Configure<IISServerOptions>(opt =>
{
    opt.MaxRequestBodySize = 512 * 1024 * 1024;

});
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

//add common service
builder.Services.AddScoped<IN.ITokenService,TokenService>();

//Add razor view global state
builder.Services.AddScoped<LayoutStateModel>();
//Add razor Js module 
builder.Services.AddScoped<ExampleJsInterop>();

/*UseSerilog configuration
 */
builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
.ReadFrom.Configuration(context.Configuration)
.ReadFrom.Services(services)
//.WriteTo.Console(new ExpressionTemplate(
//    // Include trace and span ids when present.
//    "[{@t:HH:mm:ss} {@l:u3}{#if @tr is not null} ({substring(@tr,0,4)}:{substring(@sp,0,4)}){#end}] {@m}\n{@x}"))
//
);

builder.Services.AddTransient<ProblemDetailsFactory, CustomProblemDetailsFactory>();


builder.Services.AddCommandMapper();

builder.Services.AddStore(builder.Configuration);


// Add services to the container.
//builder.Services.AddControllersWithViews();
builder.Services.AddCustomLocalization("en-US", "zh-HK");
builder.Services.AddControllersWithViews()
    .AddJsonOptions(opt => opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles)
    .AddViewLocalization( LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization(options => {
        //using same resource for data annotation for multiple classes
        options.DataAnnotationLocalizerProvider = (type, factory) =>
        factory.Create(typeof(SharedResource));
    });


builder.Services
.AddRazorComponents()
.AddInteractiveServerComponents()
.AddCircuitOptions(options => options.DetailedErrors = true); // for debugging razor components

builder.Services.AddScoped<IUrlHelper>(sp => sp.GetRequiredService<IUrlHelperFactory>().GetUrlHelper(sp.GetRequiredService<IActionContextAccessor>().ActionContext));

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = $"{typeof(Program).Assembly.GetName().Name}.Session";
});

builder.Services.AddMudServices();

// Send all exceptions to the console
MudGlobal.UnhandledExceptionHandler = (exception) => Log.Error(exception, "[{@t:HH:mm:ss} {@l:u3}{#if @tr is not null} ({substring(@tr,0,4)}:{substring(@sp,0,4)}){#end}] {@m}\n{@x}");

var app = builder.Build();


//Please don't apply to "ApplyCurrentCultureToResponseHeaders"
//otherwise, the cookie localization not work
app.UseRequestLocalization();


app.UseApiExceptionHandling();

app.MapGet("/api/hello", () => "Hello, World!");
//test successful
//app.MapGet("/api/throw", () => { throw new Exception("This is a test exception"); return Results.Ok("Not Ok"); });


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

/*Use SerilogRequestLogging
 */
app.UseSerilogRequestLogging(option =>
{
    option.EnrichDiagnosticContext = (diagnostic, http) =>
    {
        diagnostic.Set("LocalTime", DateTime.Now.ToString("yyyyMMdd+HHmmss"));

    };
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
//**this is for razor components
//**not just element for mvc
app.UseAntiforgery();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorComponents<App>()
.AddInteractiveServerRenderMode();

app.Run();
