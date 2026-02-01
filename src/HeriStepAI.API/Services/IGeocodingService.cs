namespace HeriStepAI.API.Services;

public interface IGeocodingService
{
    Task<string?> GetAddressFromCoordinatesAsync(double latitude, double longitude);
}
