using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;

namespace Schedia.Api.Google;

public sealed class GoogleAuthHelper
{
    private readonly GoogleAuthOptions _options;

    public GoogleAuthHelper(IOptions<GoogleAuthOptions> options)
    {
        _options = options.Value;
    }

    public GoogleCredential CreateCredential(string? subjectEmail = null)
    {
        var credential = LoadCredential();
        var scoped = credential.CreateScoped(_options.Scopes);

        if (_options.ImpersonationEnabled && !string.IsNullOrWhiteSpace(subjectEmail))
        {
            return scoped.CreateWithUser(subjectEmail);
        }

        return scoped;
    }

    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(_options.ServiceAccountJson) ||
               !string.IsNullOrWhiteSpace(_options.ServiceAccountJsonPath);
    }

    private GoogleCredential LoadCredential()
    {
        if (!string.IsNullOrWhiteSpace(_options.ServiceAccountJson))
        {
            return GoogleCredential.FromJson(_options.ServiceAccountJson);
        }

        if (!string.IsNullOrWhiteSpace(_options.ServiceAccountJsonPath))
        {
            return GoogleCredential.FromFile(_options.ServiceAccountJsonPath);
        }

        throw new InvalidOperationException(
            "Missing Google service account credentials. Configure GoogleAuth:ServiceAccountJsonPath or GoogleAuth:ServiceAccountJson.");
    }
}
