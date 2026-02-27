using bot.Data;
using bot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.Callbacks;

public class TaskReorderCallbackHandler(AppDbContext db) : ICallbackHandler
{
    public bool CanHandle(string data)
    {
        var parts = data.Split(':');
        return parts.Length == 3 && parts[0] == "task" && parts[2] == "reorder";
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

        var taskUsers = await db.TaskUsers
            .Where(tu => tu.TaskId == taskId && tu.IsActive)
            .Include(tu => tu.User)
            .OrderBy(tu => tu.QueuePosition)
            .ToListAsync(ct);

        await bot.EditMessageText(
            chatId: cq.Message!.Chat.Id,
            messageId: cq.Message.MessageId,
            text: "🔄 *Navbat tartibini o'zgartiring:*\n\n⬆️⬇️ tugmalar bilan tartibni o'zgartiring",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.ReorderMenu(taskId, taskUsers),
            cancellationToken: ct);
    }
}

public class TaskReorderUpCallbackHandler(AppDbContext db) : ICallbackHandler
{
    public bool CanHandle(string data)
    {
        var parts = data.Split(':');
        return parts.Length == 5 && parts[0] == "task" && parts[2] == "reorder" && parts[4] == "up";
    }

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery cq, CancellationToken ct)
    {
        var parts = cq.Data!.Split(':');
        var taskId = int.Parse(parts[1]);
        var userId = long.Parse(parts[3]);

        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (task is null || task.CreatedUserId != cq.From.Id)
        {
            await bot.AnswerCallbackQuery(cq.Id, "⛔ Ruxsat yo'q!", showAlert: true, cancellationToken: ct);
            return;
        }

        var taskUsers = await db.TaskUsers
            .Where(tu => tu.TaskId == taskId && tu.IsActive)
            .Include(tu => tu.User)
            .OrderBy(tu => tu.QueuePosition)
            .ToListAsync(ct);

        var current = taskUsers.FirstOrDefault(tu => tu.UserId == userId);
        if (current is null) return;

        var prev = taskUsers.LastOrDefault(tu => tu.QueuePosition < current.QueuePosition);
        if (prev is null)
        {
            await bot.AnswerCallbackQuery(cq.Id, "⚠️ Bu a'zo allaqachon birinchi o'rinda!", cancellationToken: ct);
            return;
        }

        var currentPos = current.QueuePosition;
        var prevPos = prev.QueuePosition;

// 1. Temp ga
        current.QueuePosition = -1;
        await db.SaveChangesAsync(ct);

// 2. prev → current eski pozitsiyasiga
        prev.QueuePosition = currentPos;
        await db.SaveChangesAsync(ct);

// 3. current → prev eski pozitsiyasiga
        current.QueuePosition = prevPos;
        await db.SaveChangesAsync(ct);

        var updated = await db.TaskUsers
            .Where(tu => tu.TaskId == taskId && tu.IsActive)
            .Include(tu => tu.User)
            .OrderBy(tu => tu.QueuePosition)
            .ToListAsync(ct);

        await bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);
        await bot.EditMessageReplyMarkup(
            chatId: cq.Message!.Chat.Id,
            messageId: cq.Message.MessageId,
            replyMarkup: BotKeyboards.ReorderMenu(taskId, updated),
            cancellationToken: ct);
    }
}

public class TaskReorderDownCallbackHandler(AppDbContext db) : ICallbackHandler
{
    public bool CanHandle(string data)
    {
        var parts = data.Split(':');
        return parts.Length == 5 && parts[0] == "task" && parts[2] == "reorder" && parts[4] == "down";
    }

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery cq, CancellationToken ct)
    {
        var parts = cq.Data!.Split(':');
        var taskId = int.Parse(parts[1]);
        var userId = long.Parse(parts[3]);

        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (task is null || task.CreatedUserId != cq.From.Id)
        {
            await bot.AnswerCallbackQuery(cq.Id, "⛔ Ruxsat yo'q!", showAlert: true, cancellationToken: ct);
            return;
        }

        var taskUsers = await db.TaskUsers
            .Where(tu => tu.TaskId == taskId && tu.IsActive)
            .Include(tu => tu.User)
            .OrderBy(tu => tu.QueuePosition)
            .ToListAsync(ct);

        var current = taskUsers.FirstOrDefault(tu => tu.UserId == userId);
        if (current is null) return;

        var next = taskUsers.FirstOrDefault(tu => tu.QueuePosition > current.QueuePosition);
        if (next is null)
        {
            await bot.AnswerCallbackQuery(cq.Id, "⚠️ Bu a'zo allaqachon oxirgi o'rinda!", cancellationToken: ct);
            return;
        }

        var currentPos = current.QueuePosition;
        var prevPos = next.QueuePosition;

// 1. Temp ga
        current.QueuePosition = -1;
        await db.SaveChangesAsync(ct);

// 2. prev → current eski pozitsiyasiga
        next.QueuePosition = currentPos;
        await db.SaveChangesAsync(ct);

// 3. current → prev eski pozitsiyasiga
        current.QueuePosition = prevPos;
        await db.SaveChangesAsync(ct);

        var updated = await db.TaskUsers
            .Where(tu => tu.TaskId == taskId && tu.IsActive)
            .Include(tu => tu.User)
            .OrderBy(tu => tu.QueuePosition)
            .ToListAsync(ct);

        await bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);
        await bot.EditMessageReplyMarkup(
            chatId: cq.Message!.Chat.Id,
            messageId: cq.Message.MessageId,
            replyMarkup: BotKeyboards.ReorderMenu(taskId, updated),
            cancellationToken: ct);
    }
}