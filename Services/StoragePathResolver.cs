namespace ConnectFour.Services;

internal static class StoragePathResolver
{
    public static string GetDataDirectory(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configuredDirectory = configuration["DataDirectory"];
        var directory = string.IsNullOrWhiteSpace(configuredDirectory)
            ? Path.Combine(environment.ContentRootPath, "App_Data")
            : Path.GetFullPath(configuredDirectory);

        Directory.CreateDirectory(directory);
        return directory;
    }
}
