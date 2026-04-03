namespace MatrixRegistrationTokenBot.Dto;

internal sealed record RegistrationTokenResponseData
{
    public required string Id { get; set; }

    public required RegistrationTokenResponseAttributes Attributes { get; set; }
}
