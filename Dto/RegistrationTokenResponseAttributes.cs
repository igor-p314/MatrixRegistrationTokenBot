namespace MatrixRegistrationTokenBot.Dto;

internal sealed record RegistrationTokenResponseAttributes
{
    public required string Token { get; set; }

    public required bool Valid { get; set; }

    public required int UsageLimit { get; set; }

    public required string CreatedAt { get; set; }

    public required string ExpiresAt { get; set; }
}
