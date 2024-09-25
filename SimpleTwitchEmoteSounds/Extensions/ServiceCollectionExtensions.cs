using Microsoft.Extensions.DependencyInjection;
using SimpleTwitchEmoteSounds.Services;
using SimpleTwitchEmoteSounds.ViewModels;

namespace SimpleTwitchEmoteSounds.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection collection)
    {
        collection.AddTransient<TwitchService>();
        collection.AddTransient<MainWindowViewModel>();
    }
}