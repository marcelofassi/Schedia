namespace Schedia.Api.Google;

public sealed class GoogleAuthOptions
{
    public string? ServiceAccountJsonPath { get; set; }
    public string? ServiceAccountJson { get; set; }
    public bool ImpersonationEnabled { get; set; }
    public string[] Scopes { get; set; } = { "https://www.googleapis.com/auth/calendar" };
}
