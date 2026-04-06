namespace Biotrackr.UI.Helpers;

public static class FormattingHelpers
{
    public static string FormatMinutes(int minutes)
    {
        if (minutes == 0) return "--";
        var h = minutes / 60;
        var m = minutes % 60;
        return h > 0 ? $"{h}h {m}m" : $"{m}m";
    }

    public static string FormatNumber(int? value)
        => value.HasValue ? value.Value.ToString("N0") : "--";

    public static string FormatDuration(long milliseconds)
    {
        var ts = TimeSpan.FromMilliseconds(milliseconds);
        return ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}h {ts.Minutes}m" : $"{ts.Minutes}m";
    }

    public static string FormatElapsedTime(int totalSeconds)
    {
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        return minutes > 0 ? $"{minutes}:{seconds:D2}" : $"{seconds}s";
    }
}
