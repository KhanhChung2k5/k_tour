namespace HeriStepAI.Mobile.Services;

public class DeviceCapabilityService : IDeviceCapabilityService
{
    // Ngưỡng: >= 4 core VÀ >= 1.5GB RAM → Mạnh
    private const int    CoreThreshold = 4;
    private const long   RamThresholdBytes = 1_500_000_000L;
    private const string PrefKey = "device_profile_v1";

    public int  CpuCores       { get; }
    public long AvailableRamMb { get; }
    public DeviceProfile Profile { get; }
    public bool IsStrong => Profile == DeviceProfile.Strong;

    public string Summary =>
        $"{(IsStrong ? "Mạnh (offline)" : "Yếu (online only)")} · {CpuCores} core · ~{AvailableRamMb}MB RAM";

    public DeviceCapabilityService()
    {
        CpuCores       = Environment.ProcessorCount;
        AvailableRamMb = GetTotalRamMb();

        Profile = CpuCores >= CoreThreshold && AvailableRamMb >= RamThresholdBytes / (1024 * 1024)
            ? DeviceProfile.Strong
            : DeviceProfile.Weak;

        Preferences.Default.Set(PrefKey, (int)Profile);
        AppLog.Info($"[DeviceCapability] {Summary}");
    }

    private static long GetTotalRamMb()
    {
#if ANDROID
        try
        {
            var activityManager = (Android.App.ActivityManager)
                Android.App.Application.Context.GetSystemService(Android.Content.Context.ActivityService)!;
            var memInfo = new Android.App.ActivityManager.MemoryInfo();
            activityManager.GetMemoryInfo(memInfo);
            return memInfo.TotalMem / (1024 * 1024);
        }
        catch { /* fallback below */ }
#endif
        return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024);
    }
}
