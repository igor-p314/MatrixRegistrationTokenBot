using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MatrixRegistrationTokenBot;

internal class TokenService
{
    private readonly string _tokenPath;

    public TokenService()
    {
        _tokenPath = Environment.GetEnvironmentVariable("MATRIX_BOT_BATCH_TOKEN_PATH")
            ?? throw new InvalidOperationException("Не задана переменная среды MATRIX_BOT_BATCH_TOKEN_PATH");
        var directory = Path.GetDirectoryName(_tokenPath)!;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    internal Task SaveAsync(string nextBatch, CancellationToken cancellationToken)
    {
        return File.WriteAllTextAsync(_tokenPath, nextBatch, cancellationToken);
    }

    internal async ValueTask<string?> GetAsync(CancellationToken cancellationToken)
    {
        string? result = File.Exists(_tokenPath)
            ? await File.ReadAllTextAsync(_tokenPath, cancellationToken).ConfigureAwait(false)
            : null;

        return result;
    }
}
