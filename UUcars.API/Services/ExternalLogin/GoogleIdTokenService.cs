using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using UUcars.API.Configurations;
using UUcars.API.Exceptions;

namespace UUcars.API.Services.ExternalLogin;

public class GoogleIdTokenService : IGoogleIdTokenService
{
    private readonly GoogleAuthSettings _settings;

    public GoogleIdTokenService(IOptions<GoogleAuthSettings> options)
    {
        _settings = options.Value;
    }

    public async Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_settings.ClientId]
            };
            return await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);
        }
        catch (InvalidJwtException)
        {
            throw new InvalidGoogleIdTokenException();
        }
    }
}
