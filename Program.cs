namespace SlnDropBuilder;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        // The installer can provision an SDK outside the system PATH.
        string sdkDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "dotnet");
        if (File.Exists(Path.Combine(sdkDirectory, "dotnet.exe")))
        {
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + Path.PathSeparator + sdkDirectory);
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }    
}