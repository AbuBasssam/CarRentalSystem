using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Helpers;
public static class QueryExecutor
{
    public static async Task<IActionResult> Execute<TQuery, TResponse>(
        TQuery query,
        ISender sender,
        Func<TResponse, IActionResult> resultBuilder)
        where TQuery : IRequest<TResponse>
    {
        var response = await sender.Send(query);
        return resultBuilder(response);
    }
}

