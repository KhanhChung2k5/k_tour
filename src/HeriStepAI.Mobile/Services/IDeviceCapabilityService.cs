namespace HeriStepAI.Mobile.Services;

public enum DeviceProfile { Strong, Weak }

public interface IDeviceCapabilityService
{
    DeviceProfile Profile { get; }
    bool IsStrong { get; }
    int CpuCores { get; }
    long AvailableRamMb { get; }
    string Summary { get; }
}
