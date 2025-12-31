using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Helpers;

public static class CommandExecutor
{
    public static async Task<IActionResult> Execute<TCommand, TResponse>(
        TCommand command,
        ISender sender,
        Func<TResponse, IActionResult> resultBuilder)
        where TCommand : IRequest<TResponse>
    {
        var response = await sender.Send(command);
        return resultBuilder(response);
    }
}

