namespace TerminalBible.Core.Storage;

public static class AppDataPaths
{
    public static string GetDefaultDataRoot()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppData))
        {
            localAppData = AppContext.BaseDirectory;
        }

        return Path.Combine(localAppData, "TerminalBible");
    }
}
