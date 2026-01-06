namespace Application.Features.AuthFeature;
public record VerificationFlowResponse
{
    public string Token { get; init; } = null!;
    public DateTime? ExpiresAt { get; init; }


}
