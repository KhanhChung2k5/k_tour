namespace HeriStepAI.Mobile;

public partial class App : Application
{
    private const string LogTag = "HeriStepAI";

    public App()
    {
        try
        {
            LogToDebug("App: InitializeComponent...");
            InitializeComponent();
            LogToDebug("App: Creating AppShell...");
            MainPage = new AppShell();
            LogToDebug("App: Started successfully");
        }
        catch (Exception ex)
        {
            var msg = $"App constructor error: {ex}\n{ex.StackTrace}";
            LogToDebug(msg);
            throw;
        }
    }

    private static void LogToDebug(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[{LogTag}] {message}");
    }
}
