using System.Collections.Generic;

namespace MatrixRegistrationTokenBot.Dto;

public sealed record InviteState
{
    public IReadOnlyCollection<RoomEvent> Events { get; set; } = [];
}
