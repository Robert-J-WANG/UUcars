using Google.Apis.Auth;

namespace UUcars.API.Services.ExternalLogin;

public interface IGoogleIdTokenService
{
    public Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken,
        CancellationToken cancellationToken = default);
}