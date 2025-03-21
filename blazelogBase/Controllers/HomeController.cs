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

namespace blazelogBase.Controllers;

public class HomeController : Controller
{
    private readonly ISender commander;
    private readonly ILogger<HomeController> _logger;
    private readonly IStringLocalizer _stringLocalizer;
    private readonly IN.ITokenService tokener;
    private readonly IMapper mapper;

    public HomeController(ILogger<HomeController> logger,IStringLocalizerFactory stringFactory,IMediator mediator,IN.ITokenService tokenHelper,IMapper itmapper )
    {
        _logger = logger;
        //using Factory instead of Dummy type blazelogBase.SharedResource as generic type of IStringLocalizer<>
        _stringLocalizer = stringFactory.Create(typeof(blazelogBase.Resources.SharedResource).Name, typeof(Program).Assembly.GetName().Name!);
        commander = mediator;
        tokener = tokenHelper;
        mapper = itmapper;
    }

    public async Task<IActionResult> Index(GetUsersQuery query)
    {
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

    public IResult Login()
    {
        return this.RazorView<Login>();
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
