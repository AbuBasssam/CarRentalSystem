using Application.Models;
using Interfaces;
using MediatR;

namespace Application.Features.AuthFeature;

public class AuthorizeUserHandler : IRequestHandler<AuthorizeUserQuery, Response<string>>
{
    #region Field(s)

    private readonly IAuthService _authService;
    private readonly ResponseHandler _responseHandler1;

    #endregion

    #region Constructor(s)
    public AuthorizeUserHandler(IAuthService authService, ResponseHandler responseHandler)
    {
        _authService = authService;
        _responseHandler1 = responseHandler;
    }
    #endregion

    #region Handler(s)

    public async Task<Response<string>> Handle(AuthorizeUserQuery request, CancellationToken cancellationToken)
    {
        bool isValid = _authService.IsValidAccessToken(request.AccessToken);
        if (isValid)
        {
            return await Task.FromResult(_responseHandler1.Success<string>("Valid"));
        }
        else
        {
            return await Task.FromResult(_responseHandler1.BadRequest<string>("Not valid"));
        }



    }

    #endregion
}
