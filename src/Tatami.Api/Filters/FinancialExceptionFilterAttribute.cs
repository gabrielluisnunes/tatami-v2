using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Tatami.Application.Financials;

namespace Tatami.Api.Filters;

public sealed class FinancialExceptionFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is FinancialException exception)
        {
            var status = exception.Code switch
            {
                "NOT_FOUND" or "ACADEMY_NOT_FOUND" => StatusCodes.Status404NotFound,
                "CONFLICT" or "ALREADY_PAID" or "INVALID_STATUS" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest,
            };
            context.Result = new ObjectResult(new { error = exception.Message, code = exception.Code }) { StatusCode = status };
            context.ExceptionHandled = true;
        }
        else if (context.Exception is UnauthorizedAccessException)
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Usuário não autenticado." });
            context.ExceptionHandled = true;
        }
    }
}