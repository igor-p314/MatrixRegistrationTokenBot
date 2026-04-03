using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MatrixRegistrationTokenBot;

internal static class HealthService
{
    internal static Task HeartBeatAsync(CancellationToken cancellationToken)
    {
#if DEBUG
        return Task.CompletedTask;
#else
        return File.WriteAllTextAsync("/tmp/heartbeat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), cancellationToken);
#endif
    }
}
