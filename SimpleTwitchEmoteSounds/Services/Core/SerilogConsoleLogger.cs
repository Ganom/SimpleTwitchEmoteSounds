#region

using Serilog;

#endregion

namespace SimpleTwitchEmoteSounds.Services.Core;

public class SerilogConsoleLogger(ILogger logger)
{
    public void WriteLine(string message)
    {
        logger.Information("[Velopack] {Message}", message);
    }

    public void WriteError(string message)
    {
        logger.Error("[Velopack] {Message}", message);
    }
}
