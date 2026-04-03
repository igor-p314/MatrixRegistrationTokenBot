namespace MatrixRegistrationTokenBot.Dto;

public sealed record MessageContent
{
    public string? MsgType { get; set; }

    public string? Body { get; set; }

    public string? Name { get; set; }
}
