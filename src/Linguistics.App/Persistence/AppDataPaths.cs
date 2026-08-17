namespace Linguistics.App.Persistence;

public sealed record AppDataPaths(string Directory, string LearnerProfileFile)
{
    public static AppDataPaths CreateDefault()
    {
        var overrideDirectory = Environment.GetEnvironmentVariable("LINGUISTICS_DATA_DIRECTORY");
        var directory = string.IsNullOrWhiteSpace(overrideDirectory)
            ? GetPlatformDataDirectory()
            : Path.GetFullPath(overrideDirectory);

        return new AppDataPaths(
            directory,
            Path.Combine(directory, "learner-profile.json"));
    }

    private static string GetPlatformDataDirectory()
    {
        if (OperatingSystem.IsMacOS())
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(
                userProfile,
                "Library",
                "Application Support",
                "com.yjay18.linguistics");
        }

        if (OperatingSystem.IsWindows())
        {
            var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localData, "com.yjay18.linguistics");
        }

        throw new PlatformNotSupportedException("Linguistics currently supports macOS and Windows.");
    }
}
