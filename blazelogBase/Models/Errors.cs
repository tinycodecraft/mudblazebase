using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace blazelogBase.Models;

public class ExceptionDetails
{
    public readonly int StatusCode;
    public readonly string Message;

    public ExceptionDetails(int statusCode, string message)
    {
        StatusCode = statusCode;
        Message = message ?? "No error message found in exception.";
    }

    public override string ToString() => JsonSerializer.Serialize(this);
}

public class DomainException : Exception
{
    public DomainException(string message, string code = null)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
public class CustomError
{
    /// <summary>
    /// The error code
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// A message from and to the Developer
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class CustomValidationProblemDetails : ValidationProblemDetails
{
    public CustomValidationProblemDetails()
    {
    }

    public CustomValidationProblemDetails(IEnumerable<CustomError> errors)
    {
        Errors = errors;
    }

    public CustomValidationProblemDetails(ModelStateDictionary modelState)
    {
        Errors = ConvertModelStateErrorsToValidationErrors(modelState);
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public new string? Detail { get; set; }

    [JsonPropertyName("errors")]
    public new IEnumerable<CustomError> Errors { get; } = new List<CustomError>();

    private List<CustomError> ConvertModelStateErrorsToValidationErrors(ModelStateDictionary modelStateDictionary)
    {
        List<CustomError> validationErrors = new();

        foreach (var keyModelStatePair in modelStateDictionary)
        {
            var errors = keyModelStatePair.Value.Errors;
            switch (errors.Count)
            {
                case 0:
                    continue;

                case 1:
                    validationErrors.Add(item: new CustomError { Code = string.Empty, Message = errors[0].ErrorMessage });
                    break;

                default:
                    var errorMessage = string.Join(Environment.NewLine, errors.Select(e => e.ErrorMessage));
                    validationErrors.Add(new CustomError { Message = errorMessage });
                    break;
            }
        }

        return validationErrors;
    }
}