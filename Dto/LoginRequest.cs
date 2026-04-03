namespace MatrixRegistrationTokenBot.Dto;

internal sealed record LoginRequest(string User, string Password, string Type = "m.login.password");