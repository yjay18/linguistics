namespace Linguistics.App;

internal static class MotionPreferences
{
    public static bool ShouldReduce(bool savedPreference) =>
        ShouldReduce(savedPreference, Environment.GetEnvironmentVariable("LINGUISTICS_REDUCED_MOTION"));

    internal static bool ShouldReduce(bool savedPreference, string? environmentValue) =>
        savedPreference || IsEnabled(environmentValue);

    private static bool IsEnabled(string? value) =>
        value is not null &&
        (value.Equals("1", StringComparison.Ordinal) ||
         value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("on", StringComparison.OrdinalIgnoreCase));
}
