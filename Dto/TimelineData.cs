using System.Collections.Generic;

namespace MatrixRegistrationTokenBot.Dto;

public sealed record TimelineData
{
    public IReadOnlyCollection<RoomEvent> Events { get; set; } = [];
}