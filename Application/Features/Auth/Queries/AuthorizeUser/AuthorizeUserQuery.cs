using Application.Models;
using MediatR;

namespace Application.Features.AuthFeature;

public class AuthorizeUserQuery : IRequest<Response<string>>
{
    public string AccessToken { get; set; } = null!;
}
