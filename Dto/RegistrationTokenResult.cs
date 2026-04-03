using System.Net;

namespace MatrixRegistrationTokenBot.Dto;

internal sealed record RegistrationTokenResult
{
    public string? Id { get; set; }

    public string? Token { get; set; }

    public bool? Valid { get; set; }

    public int? UsageLimit { get; set; }

    public string? CreatedAt { get; set; }

    public string? ExpiresAt { get; set; }

    public HttpStatusCode StatusCode { get; set; }
}
