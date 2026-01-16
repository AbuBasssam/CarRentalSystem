using Application.Models;
using MediatR;

namespace Application.Features.AuthFeature;


public record ConfirmEmailCommand(VerificationDTO dto) : IRequest<Response<bool>>;
