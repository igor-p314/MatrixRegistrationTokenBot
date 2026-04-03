using Serilog;
using System.Threading;
using System.Threading.Tasks;

namespace MatrixRegistrationTokenBot;

public class Program
{
    private static readonly CancellationTokenSource CancelTokenSource = new();

    public static async Task Main()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            var matrixService = new MatrixService();
            await matrixService.StartAsync(CancelTokenSource.Token).ConfigureAwait(false);
        }
        finally
        {
            CancelTokenSource.Dispose();
        }
    }
}
