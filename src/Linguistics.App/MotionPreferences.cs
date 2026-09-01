namespace Linguistics.App;

internal static class MotionPreferences
{
    private static readonly TimeSpan StandardPageTransition = TimeSpan.FromMilliseconds(180);

    public static bool ShouldReduce(bool savedPreference) =>
        ShouldReduce(savedPreference, Environment.GetEnvironmentVariable("LINGUISTICS_REDUCED_MOTION"));

    internal static bool ShouldReduce(bool savedPreference, string? environmentValue) =>
        savedPreference || IsEnabled(environmentValue);

    internal static TimeSpan PageTransitionDuration(bool shouldReduce) =>
        shouldReduce ? TimeSpan.Zero : StandardPageTransition;

    private static bool IsEnabled(string? value) =>
        value is not null &&
        (value.Equals("1", StringComparison.Ordinal) ||
         value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("on", StringComparison.OrdinalIgnoreCase));
}
