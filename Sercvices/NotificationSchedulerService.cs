using bot.Data;
using Microsoft.EntityFrameworkCore;

namespace bot.Sercvices;

public class NotificationSchedulerService(
    ILogger<NotificationSchedulerService> logger,
    IServiceProvider serviceProvider) : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    private static readonly TimeZoneInfo LocalZone =
        TimeZoneInfo.CreateCustomTimeZone("UZT", TimeSpan.FromHours(5), "Uzbekistan Time", "Uzbekistan Time");

    private static DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, LocalZone);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🕐 NotificationScheduler boshlandi!");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndSendNotifications(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Notification scheduler xatolik");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckAndSendNotifications(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<TaskNotificationService>();

        var localNow = LocalNow;           // e.g. 09:01 in Tashkent
        var currentTime = localNow.TimeOfDay;
        var today = localNow.Date;

        logger.LogInformation("🕐 Local time (UTC+5): {LocalNow}", localNow.ToString("dd.MM.yyyy HH:mm"));

        var tasksBase = await dbContext.Tasks
            .Where(t =>
                t.IsActive &&
                t.SendToGroup &&
                t.TelegramGroupId != null)
            .ToListAsync(ct);

        // NotifyTime is stored as local time (UTC+5), so compare directly with local currentTime
        var tasksDue = tasksBase
            .Where(t =>
                currentTime >= t.NotifyTime &&
                currentTime < t.NotifyTime + TimeSpan.FromMinutes(2) &&
                (t.LastNotifiedAt == null ||
                 // LastNotifiedAt is stored as UTC — convert to local for date comparison
                 TimeZoneInfo.ConvertTimeFromUtc(t.LastNotifiedAt.Value, LocalZone)
                     .Date.AddDays(t.NotifyIntervalDays) <= today))
            .ToList();

        logger.LogInformation("📊 {Count} ta task bildirishnoma yuborish kerak", tasksDue.Count);

        foreach (var task in tasksDue)
        {
            try
            {
                await notificationService.SendTaskNotificationById(task.Id, ct);

                // Always save LastNotifiedAt as UTC
                task.LastNotifiedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Task {TaskId} bildirishnoma xatolik", task.Id);
            }
        }
    }
}