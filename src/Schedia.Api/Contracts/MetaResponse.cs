namespace Schedia.Api.Contracts;

public sealed class MetaResponse
{
    public int HostId { get; set; }
    public string HostName { get; set; } = string.Empty;
    public string HostRoleTitle { get; set; } = string.Empty;
    public string Lang { get; set; } = "es";
    public string LegalTextVersion { get; set; } = string.Empty;
    public string LegalTextBody { get; set; } = string.Empty;
}
