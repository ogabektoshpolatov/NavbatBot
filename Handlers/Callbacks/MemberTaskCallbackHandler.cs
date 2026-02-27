using bot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot.Handlers.Callbacks;

public class MemberTaskCallbackHandler(AppDbContext db) : ICallbackHandler
{
    public bool CanHandle(string data)
    {
        var parts = data.Split(':');
        return parts.Length == 2 && parts[0] == "member_task";
    }

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery cq, CancellationToken ct)
    {
        var taskId = int.Parse(cq.Data!.Split(':')[1]);
        var userId = cq.From.Id;

        var task = await db.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId, ct);

        if (task is null)
        {
            await bot.AnswerCallbackQuery(cq.Id, "❌ Task topilmadi.", cancellationToken: ct);
            return;
        }

        var myTaskUser = await db.TaskUsers
            .FirstOrDefaultAsync(tu => tu.TaskId == taskId && tu.UserId == userId && tu.IsActive, ct);

        if (myTaskUser is null)
        {
            await bot.AnswerCallbackQuery(cq.Id, "❌ Siz bu taskda emassiz.", cancellationToken: ct);
            return;
        }

        var totalMembers = await db.TaskUsers
            .CountAsync(tu => tu.TaskId == taskId && tu.IsActive, ct);

        var currentUser = await db.TaskUsers
            .Include(tu => tu.User)
            .FirstOrDefaultAsync(tu => tu.TaskId == taskId && tu.IsCurrent, ct);

        var statusText = myTaskUser.IsCurrent
            ? "🟢 Siz navbatchisiz!"
            : myTaskUser.IsPendingConfirmation
                ? "⏳ Tasdiqlash kutilmoqda..."
                : $"📍 Sizning navbatingiz: {myTaskUser.QueuePosition}-o'rin";

        var currentText = currentUser is not null
            ? $"👤 Joriy navbatchi: *{currentUser.User.FirstName ?? "User"}*"
            : "👤 Hozircha navbatchi yo'q";

        var keyboard = myTaskUser.IsCurrent
            ? new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "✅ Navbatchilikni tugatdim",
                        $"complete_task:{taskId}:{userId}")
                }
            })
            : null;

        await bot.EditMessageText(
            chatId: cq.Message!.Chat.Id,
            messageId: cq.Message.MessageId,
            text: $"📌 *{task.Name}*\n\n" +
                  $"{currentText}\n" +
                  $"👥 Jami a'zolar: {totalMembers}\n\n" +
                  $"{statusText}",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: keyboard,
            cancellationToken: ct);
    }
}