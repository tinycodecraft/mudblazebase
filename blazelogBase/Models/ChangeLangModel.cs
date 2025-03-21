using Microsoft.AspNetCore.Mvc.Rendering;

namespace blazelogBase.Models;

public class ChangeLangModel
{
    public string? SelectedLanguage { get; set; } = "en-US";

    public bool IsSubmit { get; set; } = false;

    //view model
    public List<SelectListItem>? ListOfLanguages { get; set; }
}
