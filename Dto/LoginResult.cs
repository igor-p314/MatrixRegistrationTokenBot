namespace MatrixRegistrationTokenBot.Dto;

internal sealed record LoginResult(string AccessToken, string DeviceId, string UserId);