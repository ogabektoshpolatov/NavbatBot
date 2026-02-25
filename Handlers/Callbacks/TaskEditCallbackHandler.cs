using bot.Data;
using bot.Models;
using bot.Sercvices;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.Callbacks;

public class TaskEditMenuCallbackHandler(AppDbContext db) : ICallbackHandler
{
    public bool CanHandle(string data)
    {
        var parts = data.Split(':');
        return parts.Length == 3 && parts[0] == "task" && parts[2] == "edit";
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

        var intervalText = task.NotifyIntervalDays switch
        {
            1 => "Har kun",
            3 => "Har 3 kun",
            7 => "Har hafta",
            _ => $"Har {task.NotifyIntervalDays} kun"
        };

        await bot.EditMessageText(
            chatId: cq.Message!.Chat.Id,
            messageId: cq.Message.MessageId,
            text: $"✏️ *Tahrirlash — {task.Name}*\n\n" +
                  $"📝 Nomi: {task.Name}\n" +
                  $"📋 Tavsif: {task.Description ?? "Yo'q"}\n" +
                  $"💬 Guruh ID: {task.TelegramGroupId?.ToString() ?? "Yo'q"}\n" +
                  $"🕐 Vaqt: {task.NotifyTime:hh\\:mm}\n" +
                  $"📅 Interval: {intervalText}\n" +
                  $"🔔 Guruhga xabar: {(task.SendToGroup ? "✅" : "❌")}",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.TaskEditMenu(taskId, task.SendToGroup),
            cancellationToken: ct);
    }
}

public class TaskEditNameCallbackHandler(AppDbContext db, SessionService sessionService) : ICallbackHandler
{
    public bool CanHandle(string data)
    {
        var parts = data.Split(':');
        return parts.Length == 4 && parts[0] == "task" && parts[2] == "edit" && parts[3] == "name";
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

        var session = sessionService.GetOrCreateSession(cq.From.Id);
        session.CurrentState = BotStates.EditingTaskName;
        session.EditingTaskId = taskId;
        sessionService.UpdateSession(session);

        await bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);
        await bot.SendMessage(
            chatId: cq.Message!.Chat.Id,
            text: "📝 Yangi task nomini kiriting:",
            cancellationToken: ct);
    }
}

public class TaskEditDescriptionCallbackHandler(AppDbContext db, SessionService sessionService) : ICallbackHandler
{
    public bool CanHandle(string data)
    {
        var parts = data.Split(':');
        return parts.Length == 4 && parts[0] == "task" && parts[2] == "edit" && parts[3] == "description";
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

        var session = sessionService.GetOrCreateSession(cq.From.Id);
        session.CurrentState = BotStates.EditingTaskDescription;
        session.EditingTaskId = taskId;
        sessionService.UpdateSession(session);

        await bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);
        await bot.SendMessage(
            chatId: cq.Message!.Chat.Id,
            text: "📋 Yangi tavsifni kiriting:\n(O'chirish uchun /skip yozing)",
            cancellationToken: ct);
    }
}

public class TaskEditGroupIdCallbackHandler(AppDbContext db, SessionService sessionService) : ICallbackHandler
{
    public bool CanHandle(string data)
    {
        var parts = data.Split(':');
        return parts.Length == 4 && parts[0] == "task" && parts[2] == "edit" && parts[3] == "groupid";
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

        var session = sessionService.GetOrCreateSession(cq.From.Id);
        session.CurrentState = BotStates.EditingGroupId;
        session.EditingTaskId = taskId;
        sessionService.UpdateSession(session);

        await bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);
        await bot.SendMessage(
            chatId: cq.Message!.Chat.Id,
            text: "💬 Yangi Guruh ID ni kiriting:\n\nMasalan: -1001234567890",
            cancellationToken: ct);
    }
}

public class TaskEditNotifyTimeCallbackHandler(AppDbContext db) : ICallbackHandler
{
    public bool CanHandle(string data)
    {
        var parts = data.Split(':');
        return parts.Length == 4 && parts[0] == "task" && parts[2] == "edit" && parts[3] == "notifytime";
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
            text: "🕐 Bildirishnoma vaqtini tanlang:",
            replyMarkup: BotKeyboards.NotifyTimeSelector(taskId),
            cancellationToken: ct);
    }
}

public class TaskEditNotifyTimeSetCallbackHandler(AppDbContext db) : ICallbackHandler
{
    public bool CanHandle(string data)
    {
        var parts = data.Split(':');
        // task:1:edit:notifytime:09:00
        return parts.Length == 6 && parts[0] == "task" && parts[2] == "edit" && parts[3] == "notifytime";
    }

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery cq, CancellationToken ct)
    {
        var parts = cq.Data!.Split(':');
        var taskId = int.Parse(parts[1]);
        var timeStr = $"{parts[4]}:{parts[5]}";

        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (task is null || task.CreatedUserId != cq.From.Id)
        {
            await bot.AnswerCallbackQuery(cq.Id, "⛔ Ruxsat yo'q!", showAlert: true, cancellationToken: ct);
            return;
        }

        task.NotifyTime = TimeSpan.Parse(timeStr);
        await db.SaveChangesAsync(ct);

        await bot.AnswerCallbackQuery(cq.Id, $"✅ Vaqt {timeStr} ga o'zgartirildi!", cancellationToken: ct);
        await bot.EditMessageText(
            chatId: cq.Message!.Chat.Id,
            messageId: cq.Message.MessageId,
            text: $"✅ Bildirishnoma vaqti: *{timeStr}*",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.TaskEditMenu(taskId, task.SendToGroup),
            cancellationToken: ct);
    }
}

public class TaskEditIntervalCallbackHandler(AppDbContext db) : ICallbackHandler
{
    public bool CanHandle(string data)
    {
        var parts = data.Split(':');
        return parts.Length == 4 && parts[0] == "task" && parts[2] == "edit" && parts[3] == "interval";
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
            text: "📅 Bildirishnoma intervalini tanlang:",
            replyMarkup: BotKeyboards.IntervalSelector(taskId),
            cancellationToken: ct);
    }
}

public class TaskEditIntervalSetCallbackHandler(AppDbContext db) : ICallbackHandler
{
    public bool CanHandle(string data)
    {
        var parts = data.Split(':');
        return parts.Length == 5 && parts[0] == "task" && parts[2] == "edit" && parts[3] == "interval";
    }

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery cq, CancellationToken ct)
    {
        var parts = cq.Data!.Split(':');
        var taskId = int.Parse(parts[1]);
        var days = int.Parse(parts[4]);

        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (task is null || task.CreatedUserId != cq.From.Id)
        {
            await bot.AnswerCallbackQuery(cq.Id, "⛔ Ruxsat yo'q!", showAlert: true, cancellationToken: ct);
            return;
        }

        task.NotifyIntervalDays = days;
        await db.SaveChangesAsync(ct);

        var intervalText = days switch
        {
            1 => "Har kun",
            3 => "Har 3 kun",
            7 => "Har hafta",
            _ => $"Har {days} kun"
        };

        await bot.AnswerCallbackQuery(cq.Id, $"✅ {intervalText}!", cancellationToken: ct);
        await bot.EditMessageText(
            chatId: cq.Message!.Chat.Id,
            messageId: cq.Message.MessageId,
            text: $"✅ Interval: *{intervalText}*",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.TaskEditMenu(taskId, task.SendToGroup),
            cancellationToken: ct);
    }
}

public class TaskEditSendToGroupCallbackHandler(AppDbContext db) : ICallbackHandler
{
    public bool CanHandle(string data)
    {
        var parts = data.Split(':');
        return parts.Length == 4 && parts[0] == "task" && parts[2] == "edit" && parts[3] == "sendtogroup";
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

        if (task.TelegramGroupId is null && !task.SendToGroup)
        {
            await bot.AnswerCallbackQuery(
                cq.Id,
                "⚠️ Avval Guruh ID ni kiriting!",
                showAlert: true,
                cancellationToken: ct);
            return;
        }

        task.SendToGroup = !task.SendToGroup;
        await db.SaveChangesAsync(ct);

        await bot.AnswerCallbackQuery(
            cq.Id,
            task.SendToGroup ? "✅ Guruhga xabar yoqildi!" : "❌ Guruhga xabar o'chirildi!",
            cancellationToken: ct);

        await bot.EditMessageReplyMarkup(
            chatId: cq.Message!.Chat.Id,
            messageId: cq.Message.MessageId,
            replyMarkup: BotKeyboards.TaskEditMenu(taskId, task.SendToGroup),
            cancellationToken: ct);
    }
}