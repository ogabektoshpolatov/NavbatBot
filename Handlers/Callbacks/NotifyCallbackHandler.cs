using bot.Data;
using bot.Entities;
using bot.Models;
using bot.Sercvices;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Task = System.Threading.Tasks.Task;

namespace bot.Handlers.Callbacks;

public class NotifyCallbackHandler(
    AppDbContext dbContext,
    SessionService sessionService,
    IConfiguration config,
    ILogger<NotifyCallbackHandler> logger) : ICallbackHandler
{
    public bool CanHandle(string data) => data == "notify_yes" || data == "notify_no";

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery cq, CancellationToken ct)
    {
        var session = sessionService.GetOrCreateSession(cq.From.Id);

        if (session.CurrentState != BotStates.AwaitingAutoNotify)
        {
            await bot.AnswerCallbackQuery(cq.Id, "⚠️ /createtask dan boshlang.", cancellationToken: ct);
            return;
        }

        session.SendToGroup = cq.Data == "notify_yes";

        var task = new bot.Entities.Task
        {
            Name = session.TaskName!,
            Description = session.TaskDescription,
            TelegramGroupId = session.TelegramGroupId,
            CreatedUserId = cq.From.Id,
            NotifyTime = session.NotifyTime ?? new TimeSpan(9, 0, 0),
            NotifyIntervalDays = session.NotifyIntervalDays ?? 1,
            SendToGroup = session.SendToGroup ?? false,
            IsActive = true,
            InviteToken = Guid.NewGuid().ToString("N")[..8],
            InviteIsActive = true
        };

        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync(ct);
        
        dbContext.TaskUsers.Add(new bot.Entities.TaskUser
        {
            TaskId = task.Id,
            UserId = cq.From.Id,
            QueuePosition = 1,
            IsActive = true,
            IsCurrent = false,
            Role = TaskUserRole.Admin, 
            JoinedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Task yaratildi: {Name} by {UserId}", task.Name, cq.From.Id);

        sessionService.ClearSession(cq.From.Id);

        var botUsername = config["TelegramBot:Username"] ?? "navbatbot";
        var inviteLink = $"https://t.me/{botUsername}?start=join_{task.InviteToken}";

        await bot.AnswerCallbackQuery(cq.Id, "✅ Task yaratildi!", cancellationToken: ct);
        await bot.EditMessageText(
            chatId: cq.Message!.Chat.Id,
            messageId: cq.Message.MessageId,
            text: $"\ud83c\udf89 *Navbat muvaffaqiyatli yaratildi!*\\n\\n" +
                  $"📋 Nomi: *{task.Name}*\n" +
                  $"📅 Interval: *{(task.NotifyIntervalDays == 1 ? "Har kun" : task.NotifyIntervalDays == 3 ? "Har 3 kun" : "Har hafta")}*\n" +
                  $"🕐 Vaqt: *{task.NotifyTime:hh\\:mm}*\n" +
                  $"🔔 Guruhga xabar: *{(task.SendToGroup ? "✅ Ha" : "❌ Yo'q")}*\n\n" +
                  "─────────────────\n" +
                  "👥 *Jamoa a'zolarini qo'shish uchun:*\n" +
                  $"🔗 Quyidagi linkni ulashing:\n`{inviteLink}`\n\n" +
                  "💡 Link ustiga bosib nusxa oling!",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.TaskMenu(task.Id),
            cancellationToken: ct);
    }
}

public class CreateTimeCallbackHandler(SessionService sessionService) : ICallbackHandler
{
    public bool CanHandle(string data) => data.StartsWith("create_time:");

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery cq, CancellationToken ct)
    {
        var session = sessionService.GetOrCreateSession(cq.From.Id);
        var timeStr = cq.Data!.Replace("create_time:", "");

        // create_time:09:00 → parts
        var parts = cq.Data.Split(':');
        timeStr = $"{parts[1]}:{parts[2]}";

        session.NotifyTime = TimeSpan.Parse(timeStr);
        session.CurrentState = BotStates.AwaitingNotifyInterval;
        sessionService.UpdateSession(session);

        await bot.AnswerCallbackQuery(cq.Id, $"✅ Vaqt: {timeStr}", cancellationToken: ct);
        await bot.EditMessageText(
            chatId: cq.Message!.Chat.Id,
            messageId: cq.Message.MessageId,
            text: $"✅ Vaqt: *{timeStr}*\n\n📅 Endi intervalni tanlang:",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.IntervalSelectorCreate(),
            cancellationToken: ct);
    }
}

public class CreateIntervalCallbackHandler(SessionService sessionService) : ICallbackHandler
{
    public bool CanHandle(string data) => data.StartsWith("create_interval:");

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery cq, CancellationToken ct)
    {
        var session = sessionService.GetOrCreateSession(cq.From.Id);
        var days = int.Parse(cq.Data!.Split(':')[1]);

        session.NotifyIntervalDays = days;
        session.CurrentState = BotStates.AwaitingAutoNotify;
        sessionService.UpdateSession(session);

        var intervalText = days switch
        {
            1 => "Har kun",
            3 => "Har 3 kun",
            7 => "Har hafta",
            _ => $"Har {days} kun"
        };

        await bot.AnswerCallbackQuery(cq.Id, $"✅ {intervalText}", cancellationToken: ct);
        await bot.EditMessageText(
            chatId: cq.Message!.Chat.Id,
            messageId: cq.Message.MessageId,
            text: $"✅ Interval: *{intervalText}*\n\n" +
                  $"🔔 Guruhga avtomatik bildirishnoma yuborilsinmi?",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.NotifyYesNo(),
            cancellationToken: ct);
    }
}