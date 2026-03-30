using System.Text;
using System.Text.Json.Serialization;

namespace TrafficPilot;

internal enum AppLogLevel
{
	Debug,
	Information,
	Warning,
	Error
}

internal sealed record AppLogEntry(DateTime Timestamp, AppLogLevel Level, string Message)
{
	public string FormatForDisplay()
	{
		return $"[{Timestamp:HH:mm:ss.fff}] [{AppLogFormatting.GetShortLevelName(Level)}] {Message}";
	}

	public string FormatForFile()
	{
		return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{AppLogFormatting.GetLevelName(Level)}] {Message}";
	}
}

internal sealed class AppLoggingSettings
{
	[JsonPropertyName("enableDebug")]
	public bool EnableDebug { get; set; } = false;

	[JsonPropertyName("enableInformation")]
	public bool EnableInformation { get; set; } = true;

	[JsonPropertyName("enableWarning")]
	public bool EnableWarning { get; set; } = true;

	[JsonPropertyName("enableError")]
	public bool EnableError { get; set; } = true;

	[JsonPropertyName("writeToDirectory")]
	public bool WriteToDirectory { get; set; } = false;

	public bool IsEnabled(AppLogLevel level)
	{
		return level switch
		{
			AppLogLevel.Debug => EnableDebug,
			AppLogLevel.Information => EnableInformation,
			AppLogLevel.Warning => EnableWarning,
			AppLogLevel.Error => EnableError,
			_ => true
		};
	}
}

internal static class AppLogFormatting
{
	private const string TransportPrefix = "[TrafficPilotLog:";

	public static string Encode(AppLogLevel level, string message)
	{
		ArgumentNullException.ThrowIfNull(message);
		return $"{TransportPrefix}{GetLevelName(level)}] {message}";
	}

	public static AppLogEntry Decode(string rawMessage)
	{
		ArgumentNullException.ThrowIfNull(rawMessage);

		if (TryDecodeStructured(rawMessage, out var structured))
			return structured;

		return new AppLogEntry(DateTime.Now, InferLegacyLevel(rawMessage), rawMessage.Trim());
	}

	public static string GetLevelName(AppLogLevel level)
	{
		return level switch
		{
			AppLogLevel.Debug => "Debug",
			AppLogLevel.Information => "Information",
			AppLogLevel.Warning => "Warning",
			AppLogLevel.Error => "Error",
			_ => "Information"
		};
	}

	public static string GetShortLevelName(AppLogLevel level)
	{
		return level switch
		{
			AppLogLevel.Debug => "DBG",
			AppLogLevel.Information => "INF",
			AppLogLevel.Warning => "WRN",
			AppLogLevel.Error => "ERR",
			_ => "INF"
		};
	}

	private static bool TryDecodeStructured(string rawMessage, out AppLogEntry entry)
	{
		entry = default!;
		if (!rawMessage.StartsWith(TransportPrefix, StringComparison.Ordinal))
			return false;

		var closingBracketIndex = rawMessage.IndexOf(']');
		if (closingBracketIndex <= TransportPrefix.Length)
			return false;

		var levelToken = rawMessage[TransportPrefix.Length..closingBracketIndex];
		if (!TryParseLevel(levelToken, out var level))
			return false;

		var message = rawMessage[(closingBracketIndex + 1)..].TrimStart();
		entry = new AppLogEntry(DateTime.Now, level, message);
		return true;
	}

	private static bool TryParseLevel(string levelToken, out AppLogLevel level)
	{
		switch (levelToken)
		{
			case "Debug":
				level = AppLogLevel.Debug;
				return true;
			case "Information":
				level = AppLogLevel.Information;
				return true;
			case "Warning":
				level = AppLogLevel.Warning;
				return true;
			case "Error":
				level = AppLogLevel.Error;
				return true;
			default:
				level = AppLogLevel.Information;
				return false;
		}
	}

	private static AppLogLevel InferLegacyLevel(string rawMessage)
	{
		if (rawMessage.Contains("[error]", StringComparison.OrdinalIgnoreCase)
			|| rawMessage.Contains(" error:", StringComparison.OrdinalIgnoreCase)
			|| rawMessage.Contains(" failed", StringComparison.OrdinalIgnoreCase))
		{
			return AppLogLevel.Error;
		}

		if (rawMessage.Contains("[warn", StringComparison.OrdinalIgnoreCase)
			|| rawMessage.Contains("warning", StringComparison.OrdinalIgnoreCase))
		{
			return AppLogLevel.Warning;
		}

		if (rawMessage.Contains("[debug]", StringComparison.OrdinalIgnoreCase)
			|| rawMessage.Contains("debug:", StringComparison.OrdinalIgnoreCase))
		{
			return AppLogLevel.Debug;
		}

		return AppLogLevel.Information;
	}
}

internal sealed class AppLogFileWriter
{
	private readonly object _sync = new();

	public AppLogFileWriter(string logDirectory)
	{
		ArgumentNullException.ThrowIfNull(logDirectory);
		LogDirectory = Path.GetFullPath(logDirectory);
	}

	public string LogDirectory { get; }

	public void WriteEntries(IEnumerable<AppLogEntry> entries)
	{
		ArgumentNullException.ThrowIfNull(entries);

		var materialized = entries.ToList();
		if (materialized.Count == 0)
			return;

		lock (_sync)
		{
			Directory.CreateDirectory(LogDirectory);

			foreach (var group in materialized.GroupBy(static entry => entry.Timestamp.Date))
			{
				var path = Path.Combine(LogDirectory, $"{group.Key:yyyy-MM-dd}.log");
				using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
				using var writer = new StreamWriter(stream, new UTF8Encoding(false));
				foreach (var entry in group)
					writer.WriteLine(entry.FormatForFile());
			}
		}
	}
}
