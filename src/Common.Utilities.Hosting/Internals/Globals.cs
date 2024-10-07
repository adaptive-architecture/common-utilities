namespace AdaptArch.Common.Utilities.Hosting.Internals;

internal static class BackgroundServiceGlobals
{
    public static readonly TimeSpan OneDay = TimeSpan.FromDays(1);
    public static ConfigureAwaitOptions ConfigureAwaitOptions { get; } = ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding;
}
