using MatrixRegistrationTokenBot.Dto;
using Serilog;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MatrixRegistrationTokenBot;

internal class AuthorizationService(HttpService httpService)
{
    private readonly HttpService _httpService = httpService;

    private readonly string _login = Environment.GetEnvironmentVariable("MATRIX_BOT_USER_LOGIN")
            ?? throw new InvalidOperationException("Не задана переменная среды MATRIX_BOT_USER_LOGIN");

    private readonly string _password = Environment.GetEnvironmentVariable("MATRIX_BOT_USER_PASSWORD")
            ?? throw new InvalidOperationException("Не задана переменная среды MATRIX_BOT_USER_PASSWORD");

    internal string UserId { get; private set; } = string.Empty;

    internal async ValueTask<string> AuthorizeAsync(string url, CancellationToken cancellationToken = default)
    {
        var request = new LoginRequest(User: _login, Password: _password);

        var content = JsonContent.Create(request, Json.AppDtoContext.Default.LoginRequest);

        HttpResponseMessage response = await _httpService.PostNoRetryAsync(url, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var loginResult = JsonSerializer.Deserialize(responseString, Json.AppDtoContext.Default.LoginResult);

        var result = loginResult?.AccessToken ?? throw new InvalidOperationException("Authorization Error. Invalid Authorization Result.");
        UserId = loginResult?.UserId ?? throw new InvalidOperationException("Authorization Error. Invalid Authorization Result.");

        Log.Information("Бот {UserId} успешно авторизован в сети.", UserId);

        return result;
    }
}
