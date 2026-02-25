using bot.Data;
using bot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace bot.Sercvices;

public class TaskNotificationService(
    ILogger<TaskNotificationService> logger,
    IServiceProvider serviceProvider,
    ITelegramBotClient botClient)
{
    public async Task SendTaskNotificationById(int taskId, CancellationToken ct = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var task = await dbContext.Tasks
            .Include(t => t.TaskUsers.Where(tu => tu.IsActive))
            .ThenInclude(tu => tu.User)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct);

        if (task is null) return;
        await SendTaskNotification(task, ct);
    }

    // Eski metod — test endpoint uchun qoldiramiz
    public async Task SendDailyNotifications(CancellationToken ct = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tasks = await dbContext.Tasks
            .Where(t => t.IsActive && t.SendToGroup)
            .Include(t => t.TaskUsers.Where(tu => tu.IsActive))
            .ThenInclude(tu => tu.User)
            .ToListAsync(ct);

        foreach (var task in tasks)
        {
            try { await SendTaskNotification(task, ct); }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Task {TaskId} xatolik", task.Id);
            }
        }
    }

    private async Task SendTaskNotification(bot.Entities.Task task, CancellationToken ct)
    {
        if (!task.TaskUsers.Any())
        {
            logger.LogWarning("⚠️ Task '{Name}' da userlar yo'q", task.Name);
            return;
        }

        var allTaskUsers = task.TaskUsers
            .Where(tu => tu.IsActive)
            .OrderBy(tu => tu.QueuePosition)
            .ToList();

        var currentTaskUser = allTaskUsers.FirstOrDefault(tu => tu.IsCurrent);

        if (currentTaskUser is null)
        {
            logger.LogWarning("⚠️ Task '{Name}' da joriy navbatchi topilmadi", task.Name);
            return;
        }

        var currentUser = currentTaskUser.User;

        var userListText = string.Join("\n", allTaskUsers.Select((tu, index) =>
        {
            var user = tu.User;
            var position = index + 1;
            var userName = user.FirstName ?? user.Username ?? "User";

            if (tu.IsCurrent)
            {
                var acceptedTime = tu.UserQueueTime.HasValue
                    ? tu.UserQueueTime.Value.AddHours(5).ToString("dd.MM.yyyy HH:mm")
                    : "Noma'lum";
                return $"👉 <b>{position}. <a href=\"tg://user?id={user.UserId}\">{userName}</a></b> 🟢\n" +
                       $"    └ ⏰ <b>Qabul qilingan:</b> <code>{acceptedTime}</code>";
            }
            return $"   {position}. {userName}";
        }));

        var intervalText = task.NotifyIntervalDays switch
        {
            1 => "Har kun",
            3 => "Har 3 kun",
            7 => "Har hafta",
            _ => $"Har {task.NotifyIntervalDays} kun"
        };
        
        var message =
            $"🔔 <b>NAVBATCHILIK BILDIRISH</b>\n\n" +
            $"━━━━━━━━━━━━━━━━━━━\n" +
            $"📋 <b>Task:</b> {task.Name}\n" +
            $"📅 <b>Sana:</b> {DateTime.UtcNow.AddHours(5):dd.MM.yyyy HH:mm}\n" +
            $"📆 <b>Interval:</b> {intervalText}\n" +
            $"👥 <b>Jami navbatchilar:</b> {allTaskUsers.Count}\n" +
            $"━━━━━━━━━━━━━━━━━━━\n\n" +
            $"👤 <b>Joriy navbatchi:</b>\n" +
            $"<a href=\"tg://user?id={currentUser.UserId}\">{currentUser.FirstName ?? "User"}</a>\n\n" +
            $"━━━━━━━━━━━━━━━━━━━\n" +
            $"📋 <b>Navbat ketma-ketligi:</b>\n\n" +
            $"{userListText}\n" +
            $"━━━━━━━━━━━━━━━━━━━";
        
        await botClient.SendMessage(
            chatId: task.TelegramGroupId!.Value,
            text: message,
            parseMode: ParseMode.Html,
            replyMarkup: BotKeyboards.TaskCompletionButton(task.Id, currentUser.UserId),
            cancellationToken: ct);

        logger.LogInformation("✅ Xabar yuborildi: Task '{Name}' → {User}", task.Name, currentUser.FirstName);
    }
}