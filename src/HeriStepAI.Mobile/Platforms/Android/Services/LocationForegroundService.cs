using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace HeriStepAI.Mobile.Platforms.Android.Services;

/// <summary>
/// Foreground service to keep location tracking active when app is in background
/// </summary>
[Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
public class LocationForegroundService : Service
{
    private const int NotificationId = 1000;
    private const string ChannelId = "heristepai_location_channel";
    private const string ChannelName = "Location Tracking";

    public override IBinder? OnBind(Intent? intent)
    {
        return null; // Not a bound service
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        CreateNotificationChannel();

        var notification = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("HeriStepAI đang hoạt động")
            .SetContentText("Đang theo dõi vị trí để thuyết minh tự động")
            .SetSmallIcon(Resource.Mipmap.icon_scene_final_512)
            .SetOngoing(true)
            .SetPriority(NotificationCompat.PriorityLow)
            .Build();

        StartForeground(NotificationId, notification);

        System.Diagnostics.Debug.WriteLine("LocationForegroundService: Started");

        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        System.Diagnostics.Debug.WriteLine("LocationForegroundService: Destroyed");
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(
                ChannelId,
                ChannelName,
                NotificationImportance.Low)
            {
                Description = "Background location tracking for auto-narration"
            };

            var notificationManager = GetSystemService(NotificationService) as NotificationManager;
            notificationManager?.CreateNotificationChannel(channel);
        }
    }
}
