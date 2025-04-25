using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using blazelogBase.Models;
using blazelogBase.Middlewares;
using blazelogBase.Resources;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Localization;
using MediatR;
using blazelogBase.Store.Commands;
using blazelogBase.Store.Dtos;

using blazelogBase.Components.Pages;
using blazelogBase.Shared.Tools;
using AutoMapper;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using OpenXmlPowerTools.HtmlToWml.CSS;

namespace blazelogBase.Controllers;

public class HomeController : Controller
{
    private readonly ISender commander;
    private readonly ILogger<HomeController> _logger;
    private readonly IStringLocalizer _stringLocalizer;
    private readonly IN.ITokenService tokener;
    private readonly IMapper mapper;
    private readonly PathSetting pather;
    private readonly IN.IAuthService authService;

    //var testpath = 

    public HomeController(ILogger<HomeController> logger,IStringLocalizerFactory stringFactory,IMediator mediator,IN.ITokenService tokenHelper,IMapper itmapper,IOptions<PathSetting> paths, IN.IAuthService auth )
    {
        _logger = logger;
        //using Factory instead of Dummy type blazelogBase.SharedResource as generic type of IStringLocalizer<>
        _stringLocalizer = stringFactory.Create(typeof(blazelogBase.Resources.SharedResource).Name, typeof(Program).Assembly.GetName().Name!);
        commander = mediator;
        tokener = tokenHelper;
        mapper = itmapper;
        pather = paths.Value;
        authService = auth;
    }

    public async Task<IActionResult> Index(GetUsersQuery query)
    {
        //GlobalFilter inject the BaseUrl from HttpContextAccessor
        //ViewData[CN.Setting.BaseUrl];
        var cn = new CancellationToken();
        var user = await commander.Send( new GetUserQuery("UXKBS"),cn);
        var authuser = mapper.Map<AuthUserModel>(user);
        var token = tokener.CreateToken(authuser);
        var resultuser = tokener.DecodeTokenToUser(token);

        var result = await commander.Send(query, cn);
        return View(result);
    }

    public IActionResult Weather(int total =100)
    {

        return View(new GetWeatherForecastsQuery(total,1, 20));

    }

    public IActionResult Login()
    {
        /*
        Prerendering is the process of initially rendering page content on the server without enabling event handlers for rendered controls. The server outputs the HTML UI of the page as soon as possible in response to the initial request, which makes the app feel more responsive to users. Prerendering can also improve Search Engine Optimization (SEO) by rendering content for the initial HTTP response that search engines use to calculate page rank.
        Prerendering is enabled by default for interactive components.          
        If a parent component specifies a render mode, the prerendering settings of its children are ignored. 
        */

        return View(new LoginModel ());
    }

    [HttpPost]
    public async Task<IActionResult> Login([FromBody]LoginModel result)
    {
        var principalresult = await authService.Authenticate(result.UserId, result.Password);


        var principal = principalresult.Value as ClaimsPrincipal;



        if (principalresult.IsError || principalresult.Value == null)
        {

            return View(new LoginModel());
        }

        // settings for the authentication properties
        var authProperties = new AuthenticationProperties
        {

            //AllowRefresh = <bool>,
            // Refreshing the authentication session should be allowed.

            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1),
            // The time at which the authentication ticket expires. A
            // value set here overrides the ExpireTimeSpan option of
            // CookieAuthenticationOptions set with AddCookie.

            IsPersistent = true,
            // Whether the authentication session is persisted across
            // multiple requests. When used with cookies, controls
            // whether the cookie's lifetime is absolute (matching the
            // lifetime of the authentication ticket) or session-based.

            IssuedUtc = DateTimeOffset.UtcNow,
            // The time at which the authentication ticket was issued.

            //RedirectUri = <string>
            // The full path or absolute URI to be used as an http
            // redirect response value.
        };
        
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal!, authProperties);
        var cookieHeader = Response.GetTypedHeaders().SetCookie;
        if(cookieHeader!=null && cookieHeader.Any(y=> y.Name== CN.Setting.AuthorizeCookieKey))
        {
            var cookie = cookieHeader.FirstOrDefault(y => y.Name == CN.Setting.AuthorizeCookieKey)!;
            return Json(new CookieProps {  cookie = cookie.Value.ToString(), cookieName = CN.Setting.AuthorizeCookieKey, result = true });
        }

        return Json(new { result = false });
        
    }


    public IResult Sample(bool hideSideBar = false)
    {
        return this.RazorView<Sample>(new { HideSideBar=hideSideBar });
    }

    public IActionResult Privacy()
    {
        string? cultureCookieValue = null;
        this.HttpContext.Request.Cookies.TryGetValue(
            CookieRequestCultureProvider.DefaultCookieName, out cultureCookieValue);


        var model = ViewModelFactory.CreateViewModelWithResource<PrivacyViewModel>(_stringLocalizer);
        string text = "Thread CurrentUICulture is [" + @Thread.CurrentThread.CurrentUICulture.ToString() + "] ; ";
        text += "Thread CurrentCulture is [" + @Thread.CurrentThread.CurrentCulture.ToString() + "]";

        model.Culture = text;

        return View(model);
    }

    public IActionResult ChangeLang(ChangeLangModel model)
    {
        if(model.IsSubmit)
        {
            this.HttpContext.SetLangCookie(model.SelectedLanguage,year:1,day:0);

            return LocalRedirect("/");

        }
        model = ViewModelFactory.CreateChangeLangModel();

        return View(model);
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
