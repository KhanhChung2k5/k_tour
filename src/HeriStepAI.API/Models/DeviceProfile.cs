namespace HeriStepAI.API.Models;

public class DeviceProfileRecord
{
    public int Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public MobileDeviceProfile Profile { get; set; }
    public int? Cores { get; set; }
    public long? RamMb { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
