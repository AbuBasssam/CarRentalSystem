
using Application.Models;
using Domain.HelperClasses;
using MediatR;

namespace Application.Features.AuthFeature;

public class RefreshTokenCommand : IRequest<Response<JwtAuthResult>>
{

}
