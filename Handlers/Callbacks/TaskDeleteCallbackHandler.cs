using bot.Data;
using bot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.Callbacks;

public class TaskDeleteCallbackHandler(AppDbContext db) : ICallbackHandler
{
    public bool CanHandle(string data)
    {
        var parts = data.Split(':');
        return parts.Length == 3 && parts[0] == "task" && parts[2] == "delete";
    }

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery cq, CancellationToken ct)
    {
        var taskId = int.Parse(cq.Data!.Split(':')[1]);
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);

        if (task is null || task.CreatedUserId != cq.From.Id)
        {
            await bot.AnswerCallbackQuery(cq.Id, "⛔ Ruxsat yo'q!", showAlert: true, cancellationToken: ct);
            return;
        }

        await bot.EditMessageText(
            chatId: cq.Message!.Chat.Id,
            messageId: cq.Message.MessageId,
            text: $"🗑️ *{task.Name}* taskini o'chirishni tasdiqlaysizmi?\n\n" +
                  $"⚠️ Barcha a'zolar va ma'lumotlar o'chib ketadi!",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.DeleteConfirm(taskId),
            cancellationToken: ct);
    }
}

public class TaskDeleteConfirmCallbackHandler(AppDbContext db) : ICallbackHandler
{
    public bool CanHandle(string data)
    {
        var parts = data.Split(':');
        return parts.Length == 4 && parts[0] == "task" && parts[2] == "delete" && parts[3] == "confirm";
    }

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery cq, CancellationToken ct)
    {
        var taskId = int.Parse(cq.Data!.Split(':')[1]);
        var task = await db.Tasks
            .Include(t => t.TaskUsers)
            .ThenInclude(tu => tu.User)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct);

        if (task is null || task.CreatedUserId != cq.From.Id)
        {
            await bot.AnswerCallbackQuery(cq.Id, "⛔ Ruxsat yo'q!", showAlert: true, cancellationToken: ct);
            return;
        }

        var taskName = task.Name;

        // A'zolarga xabar
        foreach (var tu in task.TaskUsers.Where(tu => tu.UserId != cq.From.Id))
        {
            try
            {
                await bot.SendMessage(
                    chatId: tu.UserId,
                    text: $"🗑️ *{taskName}* navbati o'chirildi.\n\nTask egasi tomonidan bekor qilindi.",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: ct);
            }
            catch { }
        }

        db.Tasks.Remove(task);
        await db.SaveChangesAsync(ct);

        await bot.AnswerCallbackQuery(cq.Id, "✅ Task o'chirildi!", cancellationToken: ct);
        await bot.EditMessageText(
            chatId: cq.Message!.Chat.Id,
            messageId: cq.Message.MessageId,
            text: $"✅ *{taskName}* muvaffaqiyatli o'chirildi.",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            cancellationToken: ct);
    }
}