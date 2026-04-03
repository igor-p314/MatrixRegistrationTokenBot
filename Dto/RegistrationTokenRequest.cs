namespace MatrixRegistrationTokenBot.Dto;

internal sealed record RegistrationTokenRequest
{
    public required int UsageLimit { get; set; }

    public required string ExpiresAt { get; set; }
}
