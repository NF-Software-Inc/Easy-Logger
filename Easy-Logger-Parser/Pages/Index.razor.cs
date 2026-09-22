using easy_blazor_bulma;
using easy_core;
using Easy_Logger.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Easy_Logger_Parser.Pages;

public partial class Index : ComponentBase
{
	private readonly DataModel InputModel = new();
	private readonly FilterModel ViewModel = new();

	private readonly TooltipOptions TooltipMode = TooltipOptions.Right | TooltipOptions.HasArrow | TooltipOptions.Multiline;

	private string? GetFileTypes()
	{
		if (DeviceInfo.Platform == DevicePlatform.Android)
			return "text/plain,application/json";
		else
			return ".txt,.json,.log";
	}

	/// <summary>
    /// The maximum number of files that can be selected and processed at once.
    /// </summary>
    private const int MaxFiles = 25;

	/// <summary>
	/// Reads and parses all files selected by the user, merging their log entries into a single combined list.
	/// </summary>
	/// <param name="args">Contains the files selected by the user</param>
	private async Task AddFile(InputFileChangeEventArgs args)
	{
		var files = args.GetMultipleFiles(MaxFiles);

		if (files.Count == 0)
			return;

		if (InputModel.LogImportMode == ImportMode.Reset)
			InputModel.LogEntries.Clear();

		string? lastFileData = null;

		foreach (var file in files)
		{
			// Read the raw contents of the current file into memory
			var max = 100 * 1_048_576;

			await using var stream = file.OpenReadStream(max);
			using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
			lastFileData = await reader.ReadToEndAsync();

			// Parse the file contents and merge any resulting entries into the combined list
			if (TryParseLogFileData(lastFileData, out var entries) && entries != null)
				InputModel.LogEntries.AddRange(entries);
		}

		// Display the last selected file's raw contents
		InputModel.LogFileData = lastFileData;
		UpdateFilterMetadata();
	}

	/// <summary>
	/// Handles manual edits to the log file data, attempting to parse it and updating the filter metadata to match.
	/// </summary>
	private void OnLogFileDataChanged()
	{
		if (string.IsNullOrWhiteSpace(InputModel.LogFileData))
			return;

		if (InputModel.LogImportMode == ImportMode.Reset)
			InputModel.LogEntries.Clear();

		if (TryParseLogFileData(InputModel.LogFileData, out var parsed) && parsed != null)
			InputModel.LogEntries.AddRange(parsed);

		UpdateFilterMetadata();
	}

	/// <summary>
	/// Attempts to deserialize the provided log file data into a list of log entries, wrapping the data in an array if needed.
	/// </summary>
	/// <param name="data">The log file data to parse</param>
	/// <param name="entries">The parsed log entries if successful; otherwise, null.</param>
	/// <returns>True if data was successfully parsed; otherwise, false.</returns>
	private static bool TryParseLogFileData(string data, out List<ILoggerEntry>? entries)
	{
		try
		{
			entries = JsonSerializer.Deserialize<IEnumerable<LoggerEntryDeserializer>>(data)?.Cast<ILoggerEntry>().ToList();
			return entries != null;
		}
		catch (JsonException)
		{
			// Attempt single retry: wrap non-array JSON in brackets
			var trimmed = data.Trim().TrimEnd(['\r', '\n']).TrimEnd(',');

			if (trimmed.StartsWith('[') == false && trimmed.EndsWith(']') == false)
			{
				try
				{
					entries = JsonSerializer.Deserialize<IEnumerable<LoggerEntryDeserializer>>($"[{trimmed}]")?.Cast<ILoggerEntry>().ToList();
					return entries != null;
				}
				catch
				{
					entries = null;
					return false;
				}
			}
			else
			{
				entries = null;
				return false;
			}
		}
		catch
		{
			entries = null;
			return false;
		}
	}

	/// <summary>
	/// Recomputes the available log sources, timestamp range, and log level filters from the currently loaded log entries.
	/// </summary>
	private void UpdateFilterMetadata()
	{
		InputModel.LogSources = InputModel.LogEntries
			.Where(x => string.IsNullOrWhiteSpace(x.Source) == false)
			.Select(x => x.Source!).Distinct().ToList();

		ViewModel.Start = InputModel.LogEntries.MinOrDefault(x => x.Timestamp);
		ViewModel.End = InputModel.LogEntries.MaxOrDefault(x => x.Timestamp);
		ViewModel.SelectedLogLevels = InputModel.LogEntries.Select(x => x.Severity).Distinct().ToList();
	}

    /// <summary>
    /// Applies the current filter settings to the loaded log entries and returns the resulting list of entries to display.
    /// </summary>
    /// <returns>A list of log entries that match the current filter settings.</returns>
    private List<ILoggerEntry> GetDisplayLogEntries()
	{
        if (InputModel.LogEntries.Count == 0)
			return [];

		var predicate = PredicateBuilder.Create<ILoggerEntry>();

		if (ViewModel.Start != null)
			predicate = predicate.And(x => x.Timestamp >= ViewModel.Start.Value);

		if (ViewModel.End != null)
			predicate = predicate.And(x => x.Timestamp <= ViewModel.End.Value);

		if (ViewModel.EventNumber != null)
			predicate = predicate.And(x => x.Id != null && x.Id.Value.Id == ViewModel.EventNumber.Value);

		if (ViewModel.SelectedLogLevels.Count > 0)
			predicate = predicate.And(x => ViewModel.SelectedLogLevels.Contains(x.Severity));

		if (ViewModel.SelectedSources.Count > 0)
			predicate = predicate.And(x => x.Source != null && ViewModel.SelectedSources.Contains(x.Source, StringComparer.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(ViewModel.EventName) == false)
			predicate = predicate.And(x => x.Id != null && string.Equals(x.Id.Value.Name, ViewModel.EventName, StringComparison.OrdinalIgnoreCase));

		if (string.IsNullOrWhiteSpace(ViewModel.SearchMessage) == false)
			predicate = predicate.And(x => x.Message.Contains(ViewModel.SearchMessage, StringComparison.OrdinalIgnoreCase));

		predicate ??= PredicateBuilder.True<ILoggerEntry>();

        var property = ViewModel.SortColumn.ToLambda<ILoggerEntry>();

        if (ViewModel.SortDirection)
			return InputModel.LogEntries.Where(predicate.Compile()).AsQueryable().OrderBy(property).ToList();
		else
			return InputModel.LogEntries.Where(predicate.Compile()).AsQueryable().OrderByDescending(property).ToList();
	}

    private string GetTableHeaderCssClass(string css, string column)
	{
		if (ViewModel.SortColumn == column)
			css += " is-link";

        return string.Join(' ', css, "is-clickable is-unselectable");
	}

    private void UpdateSortValues(string column)
    {
        if (ViewModel.SortColumn == column)
            ViewModel.SortDirection = !ViewModel.SortDirection;
        else
            ViewModel.SortColumn = column;
    }

    private string? GetSortArrow(string column)
    {
        if (ViewModel.SortColumn != column)
            return "swap_vert";
        else if (ViewModel.SortDirection)
            return "arrow_upward";
        else
            return "arrow_downward";
    }

	/// <summary>
	/// Specifies how new log entries should be saved when adding files or parsing log data.
	/// </summary>
	public enum ImportMode
	{
        /// <summary>
        /// Adds new log entries to the existing ones without clearing.
        /// </summary>
		Append,
        /// <summary>
        /// Clears existing log entries before adding new ones.
        /// </summary>
		Reset
	}

    private class DataModel
	{
		[Display(Name = "Log File Data", Description = "Contains the JSON from the last log file read in the file picker")]
		public string? LogFileData { get; set; }

		public List<ILoggerEntry> LogEntries { get; set; } = [];

		[Display(Name = "Log Import Mode", Description = "Reset clears existing log entries before adding new ones. Append adds to existing entries.")]
		public ImportMode LogImportMode { get; set; } = ImportMode.Append; // Default to Append mode

		public List<string> LogSources { get; set; } = [];
	}

	private class FilterModel
	{
		[Display(Name = "Start", Description = "Filters to log entries created at or after the specified time")]
		public DateTime? Start { get; set; }

		[Display(Name = "End", Description = "Filters to log entries created at or before the specified time")]
		public DateTime? End { get; set; }

		[Display(Name = "Source", Description = "Filters to log entries with matching source values")]
		public List<string> SelectedSources { get; set; } = [];

		[Display(Name = "Log Levels", Description = "Filters to log entries with a level matching one of the selected options")]
		public List<LogLevel> SelectedLogLevels { get; set; } = [LogLevel.None];

        [Display(Name = "Event Id", Description = "Filters to log entries with a matching id value")]
		public int? EventNumber { get; set; }

		[Display(Name = "Event Name", Description = "Filters to log entries with a matching name value")]
		public string? EventName { get; set; }

		[Display(Name = "Message Text", Description = "Filters to log entries containing the provided text")]
		public string? SearchMessage { get; set; }

		public string SortColumn { get; set; } = nameof(ILoggerEntry.Timestamp);

		public bool SortDirection { get; set; }
	}

	}
