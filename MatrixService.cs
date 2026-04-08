using MatrixRegistrationTokenBot.Dto;
using MatrixRegistrationTokenBot.Matrix;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MatrixRegistrationTokenBot;

/// <summary>
/// Сервис работы с Matrix протоколом.
/// </summary>
internal class MatrixService
{
    internal static readonly string[] TokenCommands = ["!token", "!tkn"];
    internal static readonly Message TokenHelpMessage = new(
        "Для создания токена регистрации, отправьте сообщение, начинающееся на !token");

    private const int MaxAllowedUsersInRoom = 2;

    private readonly int _maxMessageAge = 14_400_000; // 4 hours in milliseconds
    private readonly string _registrationRoomKey = Environment.GetEnvironmentVariable("MATRIX_REGISTRATION_ROOM_KEY")
        ?? throw new InvalidOperationException("Не задана переменная среды MATRIX_REGISTRATION_ROOM_KEY");

    private readonly int _tokenUsageLimit = int.TryParse(
        Environment.GetEnvironmentVariable("MATRIX_BOT_TOKEN_USAGE_LIMIT"), out var parsed)
        ? parsed
        : 1;

    private readonly TokenService _tokenService = new();
    private readonly TimeProvider _timeProvider;
    private readonly HttpService _httpService = new();

    public MatrixService()
    {
        var tempString = Environment.GetEnvironmentVariable("MATRIX_BOT_MAX_MESSAGE_AGE_MS");
        if (!string.IsNullOrEmpty(tempString))
        {
            _maxMessageAge = int.Parse(tempString);
        }

        _timeProvider = TimeProvider.System;
    }

    internal async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        var authorizationService = await _httpService.AuthorizeAsync("/_matrix/client/v3/login", cancellationToken).ConfigureAwait(false);
        await ConnectToServerAsync(authorizationService, cancellationToken).ConfigureAwait(false);
    }

    private static string GetMatrixServerName(string matrixUserId)
    {
        return matrixUserId.Split(':').LastOrDefault() ?? throw new InvalidOperationException($"{matrixUserId} - неверный идентификатор пользователя.");
    }

    private async ValueTask ConnectToServerAsync(AuthorizationService authorizationService, CancellationToken cancellationToken)
    {
        string? batchFromFile = await _tokenService.GetAsync(cancellationToken).ConfigureAwait(false);
        string url;
        string nextBatch;
        if (string.IsNullOrEmpty(batchFromFile))
        {
            url = "/_matrix/client/v3/sync";
        }
        else
        {
            nextBatch = batchFromFile;
            url = $"/_matrix/client/v3/sync?since={Uri.EscapeDataString(nextBatch)}&timeout={_httpService.TimeOutMilliseconds}";
        }

        var response = await _httpService.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        nextBatch = await ProcessSyncDataResponseAsync(response, authorizationService.UserId, cancellationToken).ConfigureAwait(false);
        while (!cancellationToken.IsCancellationRequested && !string.IsNullOrEmpty(nextBatch))
        {
            try
            {
                await _tokenService.SaveAsync(nextBatch, cancellationToken).ConfigureAwait(false);
                url = $"/_matrix/client/v3/sync?since={Uri.EscapeDataString(nextBatch)}&timeout={_httpService.TimeOutMilliseconds}";
                response = await _httpService.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
                nextBatch = await ProcessSyncDataResponseAsync(response, authorizationService.UserId, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // this is fine
            }
        }

        Log.Information("Disconnected from matrix.");
    }

    private async ValueTask<string> ProcessSyncDataResponseAsync(string response, string currentUserId, CancellationToken cancellationToken)
    {
        var syncData = JsonSerializer.Deserialize(response, Json.AppDtoContext.Default.SyncUpdate)
            ?? throw new InvalidOperationException("Failed to deserialize sync data.");

        if (syncData.Rooms is not null)
        {
            if (syncData.Rooms.Invite.Count > 0)
            {
                await ProcessInvitesAsync(syncData.Rooms.Invite, cancellationToken).ConfigureAwait(false);
            }

            if (syncData.Rooms.Join.Count > 0)
            {
                var messages = syncData.Rooms.Join
                    .Where(r => r.Value.Timeline?.Events.Count > 0)
                    .SelectMany(r => r.Value.Timeline!.Events
                        .Where(e => e.Type == "m.room.message"
                                && !string.IsNullOrEmpty(e.Content.Body)
                                && !currentUserId.Equals(e.Sender, StringComparison.OrdinalIgnoreCase)
                                && _timeProvider.GetUtcNow().ToUnixTimeMilliseconds() - e.OriginServerTs < _maxMessageAge)
                    .Select(e => (roomKey: r.Key, text: e.Content.Body!, sender: e.Sender)));

                foreach (var (roomKey, text, sender) in messages)
                {
                    if (TokenCommands.Any(text.StartsWith))
                    {
                        await ProcessTokenCommandAsync(roomKey, sender, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await RespondWrongCommandAsync(roomKey, cancellationToken).ConfigureAwait(false);
                    }
                }

                foreach (var room in syncData.Rooms.Join)
                {
                    var lastEvent = room.Value.Timeline?.Events
                        .OrderByDescending(e => e.OriginServerTs)
                        .FirstOrDefault();
                    if (lastEvent?.EventId is not null)
                    {
                        await SetReadMarkerAsync(room.Key, lastEvent.EventId, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        return syncData.NextBatch ?? throw new InvalidOperationException("Sync data does not contain next batch token.");
    }

    private ValueTask ProcessInvitesAsync(Dictionary<string, InviteData> invites, CancellationToken cancellationToken)
    {
        foreach (var invite in invites)
        {
            var membersCount = invite.Value.InviteState.Events.Count(e => e.Type == "m.room.member");
            var roomName = invite.Value.InviteState.Events.FirstOrDefault(e => e.Type == "m.room.name")?.Content?.Name ?? "Unknown";
            var isEncrypted = invite.Value.InviteState.Events.Any(e => e.Type == "m.room.encryption");
            var sender = invite.Value.InviteState.Events.FirstOrDefault(e => e.Content?.Membership == "invite")?.Sender ?? string.Empty;

            if (membersCount == MaxAllowedUsersInRoom
                && !isEncrypted
                && _httpService.HomeServerUrl.Equals(GetMatrixServerName(sender), StringComparison.OrdinalIgnoreCase))
            {
                Task.Run(() => JoinDirectRoomAsync(invite.Key, cancellationToken));
            }
            else
            {
                Task.Run(() => LeaveRoomAsync(invite.Key, cancellationToken));
                Log.Information(
                    "Отклонено приглашение от {sender} в комнату '{roomName}'. Количество участников: {membersCount}, IsEncrypted = {isEncrypted}.",
                    sender,
                    roomName,
                    membersCount,
                    isEncrypted);
            }
        }

        return ValueTask.CompletedTask;
    }

    private async ValueTask JoinDirectRoomAsync(string roomKey, CancellationToken cancellationToken)
    {
        var joinUrl = $"/_matrix/client/v3/rooms/{Uri.EscapeDataString(roomKey)}/join";
        var response = await _httpService.PostAsync(joinUrl, null, cancellationToken).ConfigureAwait(false);
        Log.Information("Вход в комнату {roomKey}: {statusCode}", roomKey, response.StatusCode);
    }

    private async ValueTask LeaveRoomAsync(string roomKey, CancellationToken cancellationToken)
    {
        var leaveUrl = $"/_matrix/client/v3/rooms/{Uri.EscapeDataString(roomKey)}/leave";
        var response = await _httpService.PostAsync(leaveUrl, null, cancellationToken).ConfigureAwait(false);
        Log.Information("Уход из комнаты {roomKey}: {statusCode}", roomKey, response.StatusCode);
    }

    private ValueTask RespondWrongCommandAsync(string roomKey, CancellationToken cancellationToken)
    {
        return SendToRoomAsync(roomKey, TokenHelpMessage, cancellationToken);
    }

    private async ValueTask SendToRoomAsync(string roomKey, Message message, CancellationToken cancellationToken)
    {
        var url = $"/_matrix/client/v3/rooms/{Uri.EscapeDataString(roomKey)}/send/m.room.message";

        var content = JsonContent.Create(message.ToSerializableMessage(), Json.AppDtoContext.Default.DictionaryStringString);
        var response = await _httpService.PostAsync(url, content, cancellationToken).ConfigureAwait(false);

        Log.Information("Ответ отправлен в комнату {roomKey}: '{message}' {statusCode}", roomKey, message.MessageText, response.StatusCode);
    }

    private ValueTask ProcessTokenCommandAsync(string roomKey, string sender, CancellationToken cancellationToken)
    {
        return ProcessTokenCreationAsync(roomKey, sender, cancellationToken);
    }

    private async ValueTask ProcessTokenCreationAsync(string roomKey, string sender, CancellationToken cancellationToken)
    {
        var adminToken = await _httpService.AuthorizeAdminAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(adminToken))
        {
            Log.Information("Успешная авторизация в MAS");

            var expiresAt = _timeProvider.GetUtcNow().AddDays(1);

            var request = new RegistrationTokenRequest
            {
                Token = Guid.NewGuid().ToString("N"),
                UsageLimit = _tokenUsageLimit,
                ExpiresAt = expiresAt.ToString("o"),
            };

            var result = await _httpService.CreateRegistrationTokenAsync(request, adminToken, cancellationToken).ConfigureAwait(false);
            if (result.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK)
            {
                Log.Information("Создан токен регистрации");
                await SendTokenSuccessToRoomsAsync(roomKey, request.Token, expiresAt, sender, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                Log.Error("Ошибка создания токена регистрации: {StatusCode}", result.StatusCode);
                await SendToRoomAsync(roomKey, new Message("Ошибка создания токена. Обратитесь к администратору."), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async ValueTask SetReadMarkerAsync(string roomKey, string eventId, CancellationToken cancellationToken)
    {
        var url = $"/_matrix/client/v3/rooms/{Uri.EscapeDataString(roomKey)}/read_markers";

        var content = JsonContent.Create(
            new Dictionary<string, string>
            {
                { "m.fully_read", eventId },
                { "m.read", eventId },
            },
            Json.AppDtoContext.Default.DictionaryStringString);

        await _httpService.PostAsync(url, content, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask SendTokenSuccessToRoomsAsync(
        string roomKey,
        string registrationToken,
        DateTimeOffset expiresAt,
        string sender,
        CancellationToken cancellationToken)
    {
        var expiresAtStr = expiresAt.ToString("dd MMMM yyyy 'г.' HH:mm (UTC)");
        await SendToRoomAsync(
            roomKey,
            new FormattedMessage(
                $"Токен успешно создан. Срок действия 24 часа до <b>{expiresAtStr}</b>. Токен:",
                $"Токен успешно создан. Срок действия 24 часа до {expiresAtStr}. Токен:"),
            cancellationToken).ConfigureAwait(false);
        await SendToRoomAsync(roomKey, new Message(registrationToken), cancellationToken).ConfigureAwait(false);

        await SendToRoomAsync(
            _registrationRoomKey,
            new FormattedMessage(
                $"<a href=\"https://matrix.to/#/{sender}\">{sender}</a> создал токен <b>{registrationToken}</b>",
                $"{sender} создал токен {registrationToken}"),
            cancellationToken).ConfigureAwait(false);
    }
}
