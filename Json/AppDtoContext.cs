using MatrixRegistrationTokenBot.Dto;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MatrixRegistrationTokenBot.Json;

/// <summary>
/// Контекст Json-сериализации. Нужен для AOT.
/// </summary>
[JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(LoginResult))]
[JsonSerializable(typeof(AdminToken))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(SyncUpdate))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(RegistrationTokenRequest))]
[JsonSerializable(typeof(RegistrationTokenResponseWrapper))]
internal partial class AppDtoContext : JsonSerializerContext
{
}
