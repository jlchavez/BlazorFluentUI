using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BlazorFluentUI
{
    public static class ServiceExtension
    {
        public static void AddBlazorFluentUI(this IServiceCollection services)
        {
            services.AddScoped<IComponentStyle, ComponentStyle>();
            services.AddScoped<ThemeProvider>();
            services.AddScoped<ScopedStatics>();
            services.AddScoped<LayerHostService>();
            services.AddScoped<IFluentUISettings, FluentUISettingsSource>();
        }
    }

    public sealed class FluentUISettingsSource : IFluentUISettings
    {
        public FluentUISettingsSource(IOptions<FluentUISettings> options)
        {
            Settings = options.Value;
        }

        private const string rootPathStr = "_content/BlazorFluentUI.CoreComponents/";
        private const string basePathStr = rootPathStr + "baseComponent.js";

        private FluentUISettings Settings { get; init; }

        private string? _basePath;
        private string? _rootPath;

        public string BasePath
        {
            get => _basePath ??= string.Format("{0}{1}", Settings.BasePath ?? "./", basePathStr);
        }

        public string RootPath
        {
            get => _rootPath ??= string.Format("{0}{1}", Settings.BasePath ?? "./", rootPathStr);
        }

    }

    public interface IFluentUISettings
    {
        string BasePath { get; }
        string RootPath { get; }
    }

}