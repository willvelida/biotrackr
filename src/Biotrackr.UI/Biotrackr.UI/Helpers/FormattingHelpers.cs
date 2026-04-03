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
}
