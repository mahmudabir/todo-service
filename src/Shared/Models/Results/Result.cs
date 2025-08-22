using Shared.Extensions;
using Shared.Models.Results;

namespace Shared.Models.Results;

public class Result : ResultBase
{
    private bool _isSuccessMessage;
    private string _errorMessage;
    private string _successMessage;

    public Result() : this(false, "", new Dictionary<string, object>(), new Dictionary<string, string[]>()) { }

    internal Result(bool isSuccess = true, string? message = null, IDictionary<string, object>? additionalProperties = null, IDictionary<string, string[]>? errors = null)
    {
        Message = message;
        IsSuccess = isSuccess;
        AdditionalProperties = additionalProperties;
        Errors = errors;
    }

    public Result WithMessage(string message)
    {
        this.Message = message;
        return this;
    }

    public Result WithAdditionalProperties(IDictionary<string, object> additionalProperties)
    {
        this.AdditionalProperties = additionalProperties;
        return this;
    }

    public Result AddAdditionalProperty(KeyValuePair<string, object> additionalProperty)
    {
        this.AdditionalProperties ??= new Dictionary<string, object>();
        this.AdditionalProperties?.Add(additionalProperty.Key, additionalProperty.Value);
        return this;
    }

    public Result AddAdditionalProperties(IDictionary<string, object> additionalProperties)
    {
        foreach (var additionalProperty in additionalProperties)
        {
            AddAdditionalProperty(additionalProperty);
        }
        return this;
    }

    public Result WithError(KeyValuePair<string, string[]> error)
    {
        return AddError(error);
    }

    public Result WithErrors(IDictionary<string, string[]> errors)
    {
        foreach (var error in errors)
        {
            AddError(error);
        }
        return this;
    }

    public Result AddError(KeyValuePair<string, string[]> error)
    {
        this.Errors ??= new Dictionary<string, string[]>();
        this.Errors.AddError(error.Key, error.Value);
        return this;
    }

    public Result AddErrors(IDictionary<string, string[]> errors)
    {
        foreach (var error in errors)
        {
            AddError(error);
        }
        return this;
    }
    
    public Result WithMessageLogic(bool logic)
    {
        _isSuccessMessage = logic;
        return WithMessage(_isSuccessMessage ? _successMessage : _errorMessage);;
    }

    public Result WithSuccessMessage(string message)
    {
        _successMessage = message;
        return WithMessage(_isSuccessMessage ? _successMessage : _errorMessage);
    }

    public Result WithErrorMessage(string message)
    {
        _errorMessage = message;
        return WithMessage(_isSuccessMessage ? _successMessage : _errorMessage);
    }

    public static Result Success()
    {
        return Create(true);
    }

    public static Result Error()
    {
        return Create(false);
    }

    public static Result Error(Error error)
    {
        return Error().WithMessage(error.Message)
                      .WithErrors(error.Errors);
    }

    public static Result Create(bool isSuccessful, string? successMessage = null, string? errorMessage = null)
    {
        return Create(isSuccessful, null, null, null)
               .WithSuccessMessage(successMessage!)
               .WithErrorMessage(errorMessage!);
    }

    private static Result Create(bool isSuccessful, string? message, IDictionary<string, object>? additionalProperties = null, IDictionary<string, string[]>? errors = null)
    {
        return new Result(isSuccessful, message, additionalProperties, errors);
    }
}