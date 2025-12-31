using Application.Models;
using AutoMapper;
using Domain.Entities;
using Domain.HelperClasses;
using MediatR;

namespace Application.Features.AuthFeature;
public class SignInCommand : IRequest<Response<JwtAuthResult>>
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;

    private class Mapping : Profile
    {
        public Mapping() => CreateMap<SignInCommand, User>();
    }

}
