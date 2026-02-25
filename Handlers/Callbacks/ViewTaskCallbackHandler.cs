using bot.Data;
using bot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.Callbacks;

public class ViewTaskCallbackHandler(AppDbContext dbContext) : ICallbackHandler
{
    public bool CanHandle(string callbackData)
    {
        var parts = callbackData.Split(':');
        return parts.Length == 3 && parts[0] == "task" && parts[2] == "viewUsers";
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

        if (task.CreatedUserId != userId)
        {
            await botClient.AnswerCallbackQuery(
                callbackQuery.Id,
                "⛔ Ruxsat yo'q!",
                showAlert: true,
                cancellationToken: ct);
            return;
        }

        var taskUsers = await dbContext.TaskUsers
            .Where(tu => tu.TaskId == taskId && tu.IsActive)
            .OrderBy(tu => tu.QueuePosition)
            .Select(tu => new
            {
                tu.User.FirstName,
                tu.User.Username,
                tu.QueuePosition,
                tu.IsCurrent,
                tu.UserQueueTime,
                tu.IsPendingConfirmation
            })
            .ToListAsync(ct);

        var userCount = taskUsers.Count;

        var userListText = userCount == 0
            ? "_Hali a'zolar yo'q_"
            : string.Join("\n", taskUsers.Select((u, i) =>
            {
                var name = u.FirstName ?? u.Username ?? "User";
                if (u.IsCurrent)
                {
                    var time = u.UserQueueTime.HasValue
                        ? u.UserQueueTime.Value.AddHours(5).ToString("dd.MM HH:mm")
                        : "—";
                    return $"👉 *{i + 1}. {name}* 🟢\n    └ ⏰ `{time}`";
                }
                if (u.IsPendingConfirmation)
                    return $"   {i + 1}. {name} ⏳";

                return $"   {i + 1}. {name}";
            }));

        await botClient.EditMessageText(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: $"📌 *{task.Name}*\n\n" +
                  $"👥 *A'zolar:* {userCount}/{task.MaxMembers}\n\n" +
                  $"───────────────\n" +
                  $"👤 *Navbat ro'yxati:*\n\n" +
                  $"{userListText}\n\n" +
                  $"───────────────",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.QueueViewMenu(taskId),
            cancellationToken: ct);
    }
}