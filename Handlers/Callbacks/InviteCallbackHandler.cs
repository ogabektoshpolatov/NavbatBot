using bot.Data;
using bot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.Callbacks;

public class TaskInviteCallbackHandler(AppDbContext db, IConfiguration config) : ICallbackHandler
{
    public bool CanHandle(string data)
    {
        var parts = data.Split(':');
        return parts.Length == 3 && parts[0] == "task" && parts[2] == "invite";
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

        var botUsername = config["TelegramBot:Username"] ?? "navbatbot";
        var link = $"https://t.me/{botUsername}?start=join_{task.InviteToken}";
        var activeMembers = await db.TaskUsers.CountAsync(tu => tu.TaskId == taskId && tu.IsActive, ct);

        await bot.EditMessageText(
            chatId: cq.Message!.Chat.Id,
            messageId: cq.Message.MessageId,
            text: $"🔗 *Invite Link — {task.Name}*\n\n" +
                  $"`{link}`\n\n" +
                  $"👥 A'zolar: {activeMembers}/{task.MaxMembers}\n" +
                  $"Status: {(task.InviteIsActive ? "✅ Faol" : "🔒 Yopiq")}\n\n" +
                  $"📋 Linkni do'stlaringizga yuboring!",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.InviteMenu(taskId, task.InviteIsActive),
            cancellationToken: ct);
    }
}

public class TaskInviteRefreshCallbackHandler(AppDbContext db, IConfiguration config) : ICallbackHandler
{
    public bool CanHandle(string data)
    {
        var parts = data.Split(':');
        return parts.Length == 4 && parts[0] == "task" && parts[2] == "invite" && parts[3] == "refresh";
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

        task.InviteToken = Guid.NewGuid().ToString("N")[..8];
        await db.SaveChangesAsync(ct);

        var botUsername = config["TelegramBot:Username"] ?? "navbatbot";
        var link = $"https://t.me/{botUsername}?start=join_{task.InviteToken}";

        await bot.AnswerCallbackQuery(cq.Id, "✅ Link yangilandi!", cancellationToken: ct);
        await bot.EditMessageText(
            chatId: cq.Message!.Chat.Id,
            messageId: cq.Message.MessageId,
            text: $"🔗 *Invite Link — {task.Name}*\n\n" +
                  $"`{link}`\n\n" +
                  $"Status: {(task.InviteIsActive ? "✅ Faol" : "🔒 Yopiq")}\n\n" +
                  $"⚠️ Eski link endi ishlamaydi!",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.InviteMenu(taskId, task.InviteIsActive),
            cancellationToken: ct);
    }
}

public class TaskInviteToggleCallbackHandler(AppDbContext db, IConfiguration config) : ICallbackHandler
{
    public bool CanHandle(string data)
    {
        var parts = data.Split(':');
        return parts.Length == 4 && parts[0] == "task" && parts[2] == "invite" && parts[3] == "toggle";
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

        task.InviteIsActive = !task.InviteIsActive;
        await db.SaveChangesAsync(ct);

        var botUsername = config["TelegramBot:Username"] ?? "navbatbot";
        var link = $"https://t.me/{botUsername}?start=join_{task.InviteToken}";

        await bot.AnswerCallbackQuery(
            cq.Id,
            task.InviteIsActive ? "🔓 Link ochildi!" : "🔒 Link yopildi!",
            cancellationToken: ct);

        await bot.EditMessageText(
            chatId: cq.Message!.Chat.Id,
            messageId: cq.Message.MessageId,
            text: $"🔗 *Invite Link — {task.Name}*\n\n" +
                  $"`{link}`\n\n" +
                  $"Status: {(task.InviteIsActive ? "✅ Faol" : "🔒 Yopiq")}",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.InviteMenu(taskId, task.InviteIsActive),
            cancellationToken: ct);
    }
}