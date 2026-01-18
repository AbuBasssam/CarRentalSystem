using Application.Models;
using MediatR;

namespace Application.Features.AuthFeature;
public class SignUpCommand : IRequest<Response<bool>>
{
    public SignUpDTO Dto { get; set; }

    public SignUpCommand(SignUpDTO dto)
    {
        Dto = dto;
    }

}
