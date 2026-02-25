using bot.Data;
using Microsoft.EntityFrameworkCore;

namespace bot.Sercvices;

public class NotificationSchedulerService(
    ILogger<NotificationSchedulerService> logger,
    IServiceProvider serviceProvider) : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

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

        var now = DateTime.UtcNow;
        
        var currentTime = now.TimeOfDay;
        var today = now.Date;

        // Har bir task o'zining NotifyTime va NotifyIntervalDays ga qarab ishlaydi
        var tasksBase = await dbContext.Tasks
            .Where(t =>
                t.IsActive &&
                t.SendToGroup &&
                t.TelegramGroupId != null)
            .ToListAsync(ct);

        // 2️⃣ Time logic in memory (NO EF errors)
        var tasksDue = tasksBase
            .Where(t =>
                currentTime >= t.NotifyTime &&
                currentTime < t.NotifyTime + TimeSpan.FromMinutes(2) &&
                (t.LastNotifiedAt == null ||
                 t.LastNotifiedAt.Value.Date.AddDays(t.NotifyIntervalDays) <= today))
            .ToList();

        logger.LogInformation("📊 {Count} ta task bildirishnoma yuborish kerak", tasksDue.Count);

        foreach (var task in tasksDue)
        {
            try
            {
                await notificationService.SendTaskNotificationById(task.Id, ct);

                task.LastNotifiedAt = now;
                await dbContext.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Task {TaskId} bildirishnoma xatolik", task.Id);
            }
        }
    }
}