namespace MatrixRegistrationTokenBot.Dto;

public sealed record InviteData
{
    public required InviteState InviteState { get; set; }
}
