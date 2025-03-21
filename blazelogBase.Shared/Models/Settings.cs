
namespace blazelogBase.Shared.Models;
public class AuthSetting
{
    public string SecretKey { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

}

public class CorsPolicySetting
{
    public string Name { get; set; }
    public string[] AllowHeaders { get; set; }
    public string[] AllowMethods { get; set; }

    public string[] AllowOrigins { get; set; }

}

public class PathSetting
{
    //the share path root for keeping files
    public string Share { get; set; }
    //the upload path for keeping the uploaded files
    public string Upload { get; set; }
    //the api path for downloading
    public string Stream { get; set; }
    //the template path for keeping the template files
    public string Template { get; set; }
    //the root of web path
    public string BasePath { get; set; }
    public string BaseUrl { get; set; }


}


public class TemplateSetting
{
    //the template name for the template file
    public string User { get; set; }

}