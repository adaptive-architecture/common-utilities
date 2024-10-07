namespace AdaptArch.Common.Utilities.Hosting.Internals;

internal static class BackgroundServiceGlobals
{
    public static ConfigureAwaitOptions ConfigureAwaitOptions { get; } = ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding;
}
