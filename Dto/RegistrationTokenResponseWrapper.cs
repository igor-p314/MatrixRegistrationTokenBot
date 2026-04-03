namespace MatrixRegistrationTokenBot.Dto;

internal sealed record RegistrationTokenResponseWrapper
{
    public required RegistrationTokenResponseData Data { get; set; }
}
