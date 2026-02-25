using bot.Data;
using bot.Models;
using bot.Sercvices;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.StateHandlers;

public class EditingTaskNameHandler(SessionService service, AppDbContext db) : IStateHandler
{
    public string State => BotStates.EditingTaskName;

    public async Task HandleAsync(ITelegramBotClient bot, Message msg, UserSession session, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(msg.Text) || session.EditingTaskId is null)
        {
            await bot.SendMessage(msg.Chat.Id, "❌ Xatolik. Qaytadan urinib ko'ring.", cancellationToken: ct);
            return;
        }

        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == session.EditingTaskId, ct);
        if (task is null || task.CreatedUserId != msg.Chat.Id)
        {
            await bot.SendMessage(msg.Chat.Id, "❌ Task topilmadi.", cancellationToken: ct);
            return;
        }

        task.Name = msg.Text.Trim();
        await db.SaveChangesAsync(ct);

        session.CurrentState = null;
        session.EditingTaskId = null;
        service.UpdateSession(session);

        await bot.SendMessage(
            chatId: msg.Chat.Id,
            text: $"✅ Task nomi yangilandi: *{task.Name}*",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.TaskEditMenu(task.Id, task.SendToGroup),
            cancellationToken: ct);
    }
}

public class EditingTaskDescriptionHandler(SessionService service, AppDbContext db) : IStateHandler
{
    public string State => BotStates.EditingTaskDescription;

    public async Task HandleAsync(ITelegramBotClient bot, Message msg, UserSession session, CancellationToken ct)
    {
        if (session.EditingTaskId is null) return;

        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == session.EditingTaskId, ct);
        if (task is null || task.CreatedUserId != msg.Chat.Id) return;

        task.Description = msg.Text == "/skip" ? null : msg.Text?.Trim();
        await db.SaveChangesAsync(ct);

        session.CurrentState = null;
        session.EditingTaskId = null;
        service.UpdateSession(session);

        await bot.SendMessage(
            chatId: msg.Chat.Id,
            text: "✅ Tavsif yangilandi!",
            replyMarkup: BotKeyboards.TaskEditMenu(task.Id, task.SendToGroup),
            cancellationToken: ct);
    }
}

public class EditingGroupIdHandler(SessionService service, AppDbContext db) : IStateHandler
{
    public string State => BotStates.EditingGroupId;

    public async Task HandleAsync(ITelegramBotClient bot, Message msg, UserSession session, CancellationToken ct)
    {
        if (session.EditingTaskId is null) return;

        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == session.EditingTaskId, ct);
        if (task is null || task.CreatedUserId != msg.Chat.Id) return;

        if (!long.TryParse(msg.Text, out long groupId))
        {
            await bot.SendMessage(msg.Chat.Id, "❌ Noto'g'ri format! Raqam kiriting:", cancellationToken: ct);
            return;
        }

        task.TelegramGroupId = groupId;
        await db.SaveChangesAsync(ct);

        session.CurrentState = null;
        session.EditingTaskId = null;
        service.UpdateSession(session);

        await bot.SendMessage(
            chatId: msg.Chat.Id,
            text: $"✅ Guruh ID yangilandi: `{groupId}`",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.TaskEditMenu(task.Id, task.SendToGroup),
            cancellationToken: ct);
    }
}