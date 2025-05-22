using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentValidation.Results;
using MartinDrozdik.DDD.Models.Errors;

namespace MartinDrozdik.DDD.Models.Templates.Errors;

/// <summary>
/// Extensions for <see cref="ValidationResult"/> regarding <see cref="Error"/>s.
/// </summary>
public static class ValidationResultExtensions
{
    /// <summary>
    /// Tries to get an error from the validation result.
    /// </summary>
    /// <param name="validationResult">The result to be parsed.</param>
    /// <param name="error">Output error if the validation failed.</param>
    /// <returns>True for invalid results, else false.</returns>
    public static bool TryGetError(this ValidationResult validationResult, [NotNullWhen(true)] out Error? error)
    {
        // No error
        if (validationResult.IsValid)
        {
            error = null;
            return false;
        }

        // Build the error
        var message = validationResult.Errors.Count == 1
            ? TemplateErrorsResource.InvariantError
            : TemplateErrorsResource.InvariantErrors;

        var details = validationResult.Errors
            .Select(x => new ErrorDetail($"{x.ErrorCode}:{x.PropertyName}:{x.ErrorMessage}", x.AttemptedValue?.ToString() ?? "`null`"))
            .ToArray();

        error = new ErrorBuilder()
            .WithMessage(message)
            .WithCode(TemplateErrorCodes.InvalidValueObject)
            .WithDetails(details)
            .Build();
        return true;
    }
}
