using Application.Models;
using MediatR;

namespace Application.Features.AuthFeature;
public class SignUpCommand : IRequest<Response<string>>
{
    public SignUpCommandDTO Dto { get; set; }

    public SignUpCommand(SignUpCommandDTO dto)
    {
        Dto = dto;
    }

}
