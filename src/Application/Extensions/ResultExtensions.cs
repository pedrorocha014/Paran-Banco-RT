using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace Application.Extensions;

public static class ResultExtensions
{
    /// <summary>
    /// Converte os erros de um Result em um ValidationProblemDetails seguindo o formato RFC 9110.
    /// </summary>
    public static ValidationProblemDetails ToValidationProblemDetails(this Result result)
    {
        var errorMessages = result.Errors.Select(e => e.Message).ToArray();
        var errors = new Dictionary<string, string[]>();
        
        if (errorMessages.Length > 0)
        {
            // Usa uma chave genérica para erros de domínio/negócio
            errors["errors"] = errorMessages;
        }

        return new ValidationProblemDetails(errors)
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Title = "One or more errors occurred.",
            Status = 400
        };
    }

    /// <summary>
    /// Converte os erros de um Result&lt;T&gt; em um ValidationProblemDetails seguindo o formato RFC 9110.
    /// </summary>
    public static ValidationProblemDetails ToValidationProblemDetails<T>(this Result<T> result)
    {
        return result.ToResult().ToValidationProblemDetails();
    }
}

