using Easy_Logger.Loggers;
using Easy_Logger.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace Easy_Logger.Providers;

/// <summary>
/// Creates instances of <see cref="BrowserLogger"/> as required
/// </summary>
/// <remarks>
/// Based on <see href="https://docs.microsoft.com/en-us/dotnet/core/extensions/custom-logging-provider">Documentation</see>.
/// </remarks>
[ProviderAlias("BrowserLogger")]
public class BrowserLoggerProvider : ILoggerProvider
{
	private readonly ConcurrentDictionary<string, ILogger> Loggers = new(StringComparer.OrdinalIgnoreCase);
	private BrowserLoggerConfiguration Configuration;
	private readonly IServiceProvider Provider;
	private readonly IDisposable? OnChangeToken;

	/// <param name="configuration">The configuration to use with created loggers</param>
	/// <param name="provider">A service provider to create JS Runtime objects</param>
	public BrowserLoggerProvider(IOptionsMonitor<BrowserLoggerConfiguration> configuration, IServiceProvider provider)
	{
		Configuration = configuration.CurrentValue;
		Provider = provider;
		OnChangeToken = configuration.OnChange(updated => Configuration = updated);
	}

	private BrowserLoggerConfiguration GetConfiguration() => Configuration;

	/// <inheritdoc/>
	public ILogger CreateLogger(string categoryName)
	{
		var scope = Provider.CreateScope();
		var runtime = scope.ServiceProvider.GetService<IJSRuntime>();

		if (runtime == null)
			throw new JSException("Failed creating JS Runtime object for logger");

		return Loggers.GetOrAdd(categoryName, source => new BrowserLogger(source, runtime, GetConfiguration));
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		Loggers.Clear();
		OnChangeToken?.Dispose();

		GC.SuppressFinalize(this);
	}
}

/// <summary>
/// Implementation of <see cref="ILoggerConfiguration"/> for <see cref="BrowserLogger"/>
/// </summary>
public class BrowserLoggerConfiguration : ILoggerConfiguration
{
	/// <inheritdoc/>
	public LogLevel[] LogLevels { get; set; } = [LogLevel.Trace, LogLevel.Debug, LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Critical];

	/// <inheritdoc/>
	public List<string> IgnoredMessages { get; set; } = [];

	/// <inheritdoc/>
	public Func<ILoggerEntry, string>? Formatter { get; set; } = entry =>
	{
		return $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}; Severity={entry.Severity}; Source={entry.Source}" + Environment.NewLine + entry.Message + Environment.NewLine;
	};

	/// <summary>
	/// A collection of CSS styles to apply to messages. Supports up to 5 styles.
	/// </summary>
	/// <remarks>
	/// You must set <see cref="Formatter"/> for this to work. For each item in the list, you will need to add a '%c' to use it in your messages. Example:
	///
	/// <code>
	/// Formatter = entry => $"%c{entry.Source} %c{entry.Message}"
	/// CssStyles = new() { "color:#0ff", "color:#000; font-size:1.5rem" }
	/// </code>
	///
	/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/console#styling_console_output">Documentation</see>
	/// </remarks>
	public List<string>? CssStyles { get; set; }
}

/// <summary>
/// Contains methods to consume <see cref="BrowserLogger"/> in a DI environment
/// </summary>
public static class BrowserLoggerExtensions
{
	/// <summary>
	/// Adds <see cref="BrowserLogger"/> to the service collection with default options
	/// </summary>
	/// <param name="builder">The builder containing the service collection</param>
	public static ILoggingBuilder AddBrowserLogger(this ILoggingBuilder builder)
	{
		builder.AddConfiguration();
		builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, BrowserLoggerProvider>());

		LoggerProviderOptions.RegisterProviderOptions<BrowserLoggerConfiguration, BrowserLoggerProvider>(builder.Services);
		builder.Services.Configure<BrowserLoggerConfiguration>(x => new BrowserLoggerConfiguration());

		return builder;
	}

	/// <summary>
	/// Adds <see cref="BrowserLogger"/> to the service collection with the provided options
	/// </summary>
	/// <param name="builder">The builder containing the service collection</param>
	/// <param name="configure">The options to use for created loggers</param>
	public static ILoggingBuilder AddBrowserLogger(this ILoggingBuilder builder, Action<BrowserLoggerConfiguration> configure)
	{
		builder.AddConfiguration();
		builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, BrowserLoggerProvider>());

		LoggerProviderOptions.RegisterProviderOptions<BrowserLoggerConfiguration, BrowserLoggerProvider>(builder.Services);
		builder.Services.Configure(configure);

		return builder;
	}
}
