using bot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot.Sercvices;

public class TaskUiRenderer(AppDbContext dbContext)
{
    public async Task RenderTaskWithUsersAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        int taskId,
        InlineKeyboardMarkup keyboard,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        var users = await dbContext.TaskUsers
            .Where(tu => tu.TaskId == taskId && tu.IsActive)
            .Select(tu => tu.User.FirstName)
            .ToListAsync(cancellationToken);

        var userCount = users.Count;

        var userListText = users.Any()
            ? string.Join("\n", users.Select((u, i) => $"{i + 1}. {u ?? "User"}"))
            : "_Hali a'zolar yo'q_";

        var intervalText = task!.NotifyIntervalDays switch
        {
            1 => "Har kun",
            3 => "Har 3 kun",
            7 => "Har hafta",
            _ => $"Har {task.NotifyIntervalDays} kun"
        };

        await botClient.EditMessageText(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: $"📌 *{task.Name}*\n" +
                  $"👥 A'zolar: {userCount}/{task.MaxMembers}\n" +
                  $"📅 Interval: {intervalText}\n" +
                  $"🕐 Vaqt: {task.NotifyTime:hh\\:mm}\n\n" +
                  $"👤 *A'zolar ro'yxati:*\n{userListText}",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }
}