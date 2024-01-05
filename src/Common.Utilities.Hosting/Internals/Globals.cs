namespace AdaptArch.Common.Utilities.Hosting.Internals;

internal static class BackgroundServiceGlobals
{
    public static TimeSpan CheckEnabledPollingInterval { get; set; } = TimeSpan.FromHours(1);
}
