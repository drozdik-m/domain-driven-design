using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;
using FluentValidation;
using FluentValidation.Results;
using MartinDrozdik.DDD.Errors.WellKnown;
using MartinDrozdik.DDD.Models.Errors;
using MartinDrozdik.DDD.Models.Errors.WellKnown;
using MartinDrozdik.DDD.Models.Exceptions;

namespace MartinDrozdik.DDD.Models.Extensions;

/// <summary>
/// Extensions for <see cref="ValidationResult"/> regarding <see cref="Error"/>s and exceptions.
/// </summary>
public static class ValidationExtensions
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
        error = validationResult.Errors.GetError();
        return true;
    }

    /// <summary>
    /// Converts a collection of <see cref="ValidationFailure"/> to an <see cref="Error"/>.
    /// </summary>
    /// <param name="failures">List of failures.</param>
    /// <returns>New <see cref="Error"/>.</returns>
    public static Error GetError(this IEnumerable<ValidationFailure> failures)
    {
        // Build the error
        var message = failures.Count() == 1
            ? WellKnownErrorsResource.InvariantError
            : WellKnownErrorsResource.InvariantErrors;

        var details = failures
            .Select(x => new ErrorDetail(x.PropertyName, $"{x.ErrorCode}: {x.ErrorMessage} {x.AttemptedValue?.ToString() ?? "`null`"}"))
            .ToArray();

        return new ErrorBuilder()
            .WithMessage(message)
            .WithCode(ErrorCodes.InvalidObject)
            .WithDetails(details)
            .Build();
    }

    /// <summary>
    /// Converts a collection of <see cref="ValidationFailure"/> to a <see cref="BusinessRuleValidationException"/>.
    /// </summary>
    /// <param name="failures">List of failures.</param>
    /// <returns>New <see cref="Error"/>.</returns>
    public static BusinessRuleValidationException GetException(this IEnumerable<ValidationFailure> failures)
    {
        var error = failures.GetError();
        return error.ToValidationException();
    }

    /// <summary>
    /// Converts an <see cref="Error"/> to a <see cref="BusinessRuleException"/>.
    /// </summary>
    /// <param name="error">The <see cref="Error"/> to convert.</param>
    /// <returns>New <see cref="BusinessRuleException"/>.</returns>
    public static BusinessRuleException ToBusinessRuleException(this Error error)
    {
        return new BusinessRuleException(error.Message, error.Exception)
        {
            Details = error.Details.Select(ToExceptionDetail),
        };
    }

    /// <summary>
    /// Converts an <see cref="Error"/> to a <see cref="BusinessRuleValidationException"/>.
    /// </summary>
    /// <param name="error">The <see cref="Error"/> to convert.</param>
    /// <returns>New <see cref="BusinessRuleValidationException"/>.</returns>
    public static BusinessRuleValidationException ToValidationException(this Error error)
    {
        return new BusinessRuleValidationException(error.Message, error.Exception)
        {
            Details = error.Details.Select(ToExceptionDetail),
        };
    }

    /// <summary>
    /// Tries to get an exception from the validation result.
    /// </summary>
    /// <param name="validationResult">The result to be parsed.</param>
    /// <param name="exception">Output exception if the validation failed.</param>
    /// <returns>True for invalid results, else false.</returns>
    public static bool TryGetException(this ValidationResult validationResult, [NotNullWhen(true)] out BusinessRuleValidationException? exception)
    {
        if (validationResult.TryGetError(out var error))
        {
            exception = error.ToValidationException();
            return true;
        }

        exception = null;
        return false;
    }

    /// <summary>
    /// Builds and returns a <see cref="BusinessRuleException"/>.
    /// </summary>
    /// <param name="errorBuilder">The source <see cref="ErrorBuilder"/>.</param>
    /// <returns>New <see cref="BusinessRuleException"/>.</returns>
    public static BusinessRuleException BuildBusinessException(this ErrorBuilder errorBuilder)
    {
        var error = errorBuilder.Build();
        return error.ToBusinessRuleException();
    }

    /// <summary>
    /// Builds and throws a <see cref="BusinessRuleValidationException"/>.
    /// </summary>
    /// <param name="errorBuilder">The source <see cref="ErrorBuilder"/>.</param>
    /// <returns>New <see cref="BusinessRuleValidationException"/>.</returns>
    public static BusinessRuleValidationException BuildValidationException(this ErrorBuilder errorBuilder)
    {
        var error = errorBuilder.Build();
        return error.ToValidationException();
    }

    /// <summary>
    /// Performs validation and then throws a <see cref="BusinessRuleValidationException"/> if validation fails.
    /// </summary>
    /// <typeparam name="T">The type we are validating.</typeparam>
    /// <param name="validator">The validator this method is extending.</param>
    /// <param name="instance">The instance of the type we are validating.</param>
    public static void ValidateAndThrowBusiness<T>(this IValidator<T> validator, T instance)
    {
        var result = validator.Validate(instance);
        if (result.TryGetException(out var exception))
        {
            throw exception;
        }
    }

    /// <summary>
    /// Performs validation asynchronously and then throws a <see cref="BusinessRuleValidationException"/> if validation fails.
    /// </summary>
    /// <typeparam name="T">The type we are validating.</typeparam>
    /// <param name="validator">The validator this method is extending.</param>
    /// <param name="instance">The instance of the type we are validating.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task ValidateAndThrowBusinessAsync<T>(this IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (result.TryGetException(out var exception))
        {
            throw exception;
        }
    }

    /// <summary>
    /// Converts an <see cref="ErrorDetail"/> to an <see cref="ExceptionDetail"/>.
    /// </summary>
    /// <param name="errorDetail">The error to convert.</param>
    /// <returns>New converted <see cref="ExceptionDetail"/>.</returns>
    private static ExceptionDetail ToExceptionDetail(this ErrorDetail errorDetail)
    {
        return new ExceptionDetail(errorDetail.Key, errorDetail.Value);
    }
}
