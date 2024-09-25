using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace SimpleTwitchEmoteSounds.Common;

public static class Debouncer
{
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> Tokens = new();

    public static void Debounce(string uniqueKey, Action action, int milliseconds = 300)
    {
        var token = Tokens.AddOrUpdate(uniqueKey,
            _ => new CancellationTokenSource(),
            (_, existingToken) =>
            {
                existingToken.Cancel();
                return new CancellationTokenSource();
            }
        );

        Task.Delay(milliseconds, token.Token).ContinueWith(task =>
        {
            if (task.IsCanceled) return;
            Dispatcher.UIThread.InvokeAsync(action);
            if (Tokens.TryRemove(uniqueKey, out var cts)) cts.Dispose();
        }, token.Token);
    }
}