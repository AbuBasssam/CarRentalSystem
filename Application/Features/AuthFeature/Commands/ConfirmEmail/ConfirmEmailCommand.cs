using Application.Models;
using MediatR;

namespace Application.Features.AuthFeature;


public record ConfirmEmailCommand(ConfirmEmailDTO dto) : IRequest<Response<bool>>;
