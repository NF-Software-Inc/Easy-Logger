# Easy Logger

[![MIT](https://img.shields.io/github/license/NF-Software-Inc/Easy-Logger)](https://github.com/NF-Software-Inc/Easy-Logger/blob/master/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Easy.Log.Writer.svg)](https://www.nuget.org/packages/Easy.Log.Writer/)
[![Build](https://img.shields.io/github/actions/workflow/status/NF-Software-Inc/Easy-Logger/build.yml)](https://github.com/NF-Software-Inc/Easy-Logger/actions/workflows/build.yml)
[![Publish](https://img.shields.io/github/actions/workflow/status/NF-Software-Inc/Easy-Logger/publish.yml?label=publish)](https://github.com/NF-Software-Inc/Easy-Logger/actions/workflows/publish.yml)

This project was created to provide an easy to use and configurable logging library. If the default configuration is sufficient for your needs the library can be used out of the box without further setup. However, if you have specific logging needs you can alter the library configuration settings and also provide custom logging methods.

The default implementation is capable of:

- Recording to text log files
- Recording to JSON log files
- Recording to an in-memory dictionary
- Recording to the console
- Recording to the web console (requires use of the Easy.Log.Writer.Blazor package or Easy-Logger-Blazor.csproj project for coloring and formatting log messages)
- Recording to a SQL Server (requires use of the Easy.Log.Writer.Sql package or Easy-Logger-Sql.csproj project)
- Adding dated folders to text-based logs (ex. /logs/2020/05/01/log.txt)
- Adding templated filenames to text-based logs (ex. /logs/2020-05-01_My.Namespace_Log_150059.txt)
- Being used as an ILogger implementation for ASP.NET and other API type applications
- Parsing and searching JSON log files (requires downloading and installing the Easy Logger Parser app)

The V2 edition has been released to provide a more standards based logging system. The loggers and providers are now using the default `ILogger` and `ILoggerProvider` implementations. It is recommended to register your required loggers using the providers and then using dependency injection to get `ILogger` instances as required. You can still directly create loggers if required.

## Getting Started

These instuctions can be used to acquire and implement the library.

### Installation

To use this library:

* Clone a copy of the repository
* Reference the [Main NuGet package](https://www.nuget.org/packages/Easy.Log.Writer/), [Blazor NuGet package](https://www.nuget.org/packages/Easy.Log.Writer.Blazor/), or [SQL Server NuGet package](https://www.nuget.org/packages/Easy.Log.Writer.Sql/)
* Try it on Android with the [Play Store App](https://play.google.com/store/apps/details?id=com.nf_software_inc.easy_logger_parser) or APK file (see releases)
* Try it on iOS with the [App Store App](https://apps.apple.com/ca/app/easy-logger-parser/id6738100992)
* Try it on Windows with the [Microsoft Store App](https://apps.microsoft.com/detail/9nbmgcqm9fw1) or MSIX package (see releases)

### Usage

**Recommended Example**

The following provides an example of the recommended usage. 

In this example both the text logger and console loggers are configured, so any log messages will be recorded to both. The text logger is configured using a custom formatter that will be applied as logs are saved to the text files; whereas the console logger has not been configured using a custom formatter, so the default formatter will be used.

Program.cs or configuration class

```csharp
var builder = WebApplication.CreateBuilder();

var logLevels = new[] { LogLevel.Trace, LogLevel.Debug, LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Critical };
var ignoredMessages = new List<string>() { "TaskCanceledException" };

builder.Logging
    .ClearProviders()
    .AddTextLogger(options =>
    {
        options.LogLevels = logLevels;
        options.IgnoredMessages = ignoredMessages;
        options.LogDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
        options.LogfileNameTemplate = "[Date:yyyy]-myapplog-[Date:MM-dd_HH]";
        options.SubdirectoryMode = DatedSubdirectoryModes.Daily;

        options.Formatter = entry =>
        {
            return $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}; Severity={entry.Severity}; Source={entry.Source}; Text={entry.Message}";
        };
    })
    .AddConsoleLogger(options => 
    {
        options.LogLevels = logLevels;
        options.IgnoredMessages = ignoredMessages;

        options.LogLevelToColorMap = new Dictionary<LogLevel, ConsoleColor>()
        {
            [LogLevel.Trace] = ConsoleColor.Cyan,
            [LogLevel.Debug] = ConsoleColor.Blue,
            [LogLevel.Information] = ConsoleColor.Green,
            [LogLevel.Warning] = ConsoleColor.Yellow,
            [LogLevel.Error] = ConsoleColor.Red,
            [LogLevel.Critical] = ConsoleColor.Magenta
        };
    });
```

Controller or other service

```csharp
public sealed class InfoController : ControllerBase 
{
    private readonly ILogger logger;

    public InfoController(ILogger<InfoController> logger) 
    {
        this.logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IApiResponse<bool>), StatusCodes.Status200OK)]
    public IActionResult Test() 
    {
        logger.LogInformation($"{nameof(Test)} function queried.");
        return Ok(true);
    }
}
```

**Per-Message Formatting Example**

The following provides an example of customizing the formatting for a single message. The per-message formatting is applied to the ILoggerEntry.Message property; if the configuration for the logger has a global formatter, that will be applied afterwards.

```csharp
public void Test(string? message)
{
    try 
    {
        if (string.IsNullOrWhitespace(message))
            throw new ArgumentException("must provide a message", nameof(message));

        logger.Log(LogLevel.Information, new EventId(), message, null, (state, exception) => $"{nameof(Test)} function succeeded with message {state}.");
    }
    catch (Exception e)
    {
        logger.Log<object?>(LogLevel.Error, new EventId(12345), null, e, (state, exception) => $"{nameof(Test)} function failed with {exception.GetType().Name} {exception.Message}.");
    }
}
```

**Direct Example**

The following provides an example of using a logger directly.

```csharp
Console.WriteLine("Enter a message to log:");
var message = Console.ReadLine();

var logger = new ConsoleLogger("", () => new LogLevel[] { LogLevel.Information, LogLevel.Warning }, new List<string>() { "TaskCanceledException" }, new Dictionary<LogLevel, ConsoleColor>());
logger.LogInformation(input);
```

**Blazor WebAssembly Example**

The following example displays how the memory logger can be used to download a file in Blazor WebAssembly. This method is also useful in other situations where file system access is either unavailable or restricted. The example provides a button that users can click to save the logs; this has proven useful over console logging as the user can save logs then navigate elsewhere.

Program.cs or configuration class

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Logging.AddMemoryLogger(x =>
{
	x.LogLevels = new[] { LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Critical };
});
```

Blazor Component

```razor
<button type="button" @onclick=OnClickSaveReport>Save Logs</button>

@code {
    [Inject]
	private IJSRuntime JsRuntime { get; init; }

    private async void OnClickSaveReport()
	{
		// Generate file content
		var text = JsonSerializer.Serialize(MemoryLoggerProvider.DefaultLogger.Select(x => x.Value).OrderBy(x => x.Timestamp));
		var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_debug.txt";

		// Stream
		var bytes = Encoding.ASCII.GetBytes(text);
		var fileStream = new MemoryStream(bytes);

		using var streamRef = new DotNetStreamReference(fileStream);
		await JsRuntime.InvokeVoidAsync("DownloadFileFromStream", fileName, streamRef);
	}
}
```

JavaScript Function

```js
/**
 * Converts the provided stream to a blob in memory and downloads the file
 * @param {string} fileName The name to assign the downloaded file
 * @param {any} contentStreamReference A reference to the steam to download
 */
DownloadFileFromStream: async function (fileName, contentStreamReference) {
    var arrayBuffer = await contentStreamReference.arrayBuffer();
    var blob = new Blob([arrayBuffer]);
    var url = URL.createObjectURL(blob);

    this.DownloadFileFromUrl(fileName, url);

    URL.revokeObjectURL(url);
}
```

**Blazor Browser Console Example**

The following example displays how to configure the console logger in a web browser. This uses the Blazor NuGet package.

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Logging.AddBrowserLogger(x =>
{
	x.LogLevels = new[] { LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Critical };
});
```

## Authors

* **NF Software Inc.**

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details

## Acknowledgments

Thank you to:
* [Kmg Design](https://www.iconfinder.com/kmgdesignid) for the project icon