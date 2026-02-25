using bot.Data;
using bot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.Callbacks;

public class TaskMenuCallbackHandler(AppDbContext dbContext) : ICallbackHandler
{
    public bool CanHandle(string callbackData)
    {
        var parts = callbackData.Split(':');
        return parts.Length == 2 && parts[0] == "task";
    }

    public async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var taskId = int.Parse(callbackQuery.Data!.Split(':')[1]);
        var userId = callbackQuery.From.Id;

        var task = await dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId, ct);

        if (task is null)
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "❌ Task topilmadi.", cancellationToken: ct);
            return;
        }

        // Faqat owner ko'ra oladi
        if (task.CreatedUserId != userId)
        {
            await botClient.AnswerCallbackQuery(
                callbackQuery.Id,
                "⛔ Ruxsat yo'q!",
                showAlert: true,
                cancellationToken: ct);
            return;
        }

        var userCount = await dbContext.TaskUsers
            .CountAsync(tu => tu.TaskId == taskId && tu.IsActive, ct);

        var intervalText = task.NotifyIntervalDays switch
        {
            1 => "Har kun",
            3 => "Har 3 kun",
            7 => "Har hafta",
            _ => $"Har {task.NotifyIntervalDays} kun"
        };

        await botClient.EditMessageText(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: $"📌 *{task.Name}*\n\n" +
                  $"👥 A'zolar: {userCount}/{task.MaxMembers}\n" +
                  $"📅 Interval: {intervalText}\n" +
                  $"🕐 Vaqt: {task.NotifyTime:hh\\:mm}\n" +
                  $"🔔 Guruhga xabar: {(task.SendToGroup ? "✅" : "❌")}\n" +
                  $"🔗 Invite: {(task.InviteIsActive ? "✅ Faol" : "🔒 Yopiq")}",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.TaskMenu(taskId),
            cancellationToken: ct);
    }
}