using bot.Data;
using bot.Models;
using bot.Sercvices;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.Callbacks;

public class NotifyCallbackHandler(
    AppDbContext dbContext, 
    SessionService sessionService, 
    ILogger<NotifyCallbackHandler> logger) : ICallbackHandler
{
    public bool CanHandle(string callbackData)
    {
        return callbackData == "notify_yes" || callbackData == "notify_no";
    }
    
    public async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var callback = callbackQuery.Data;
        var session = sessionService.GetOrCreateSession(callbackQuery.From.Id);
        
        if (session.CurrentState != BotStates.AwaitingAutoNotify)
        {
            await botClient.AnswerCallbackQuery(
                callbackQueryId: callbackQuery.Id,
                text: "⚠️ Bu tugma hozir ishlamaydi. Iltimos, /createtask dan boshlang.",
                cancellationToken: cancellationToken);
            return;
        }
        
        session.SendToGroup = callback == "notify_yes";
        
        var task = new Entities.Task
        {
            Name = session.TaskName,
            ScheduleTime = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            TelegramGroupId = session.TelegramGroupId ?? 0,
            CreatedUserId = session.UserId,
            IsActive = true,
            SendToGroup = session.SendToGroup ?? false
        };

        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("✅ Task '{TaskName}' created by user {UserId}", session.TaskName, session.UserId);
        
        sessionService.ClearSession(session.UserId);
        
        await botClient.AnswerCallbackQuery(
            callbackQueryId: callbackQuery.Id,
            text: "✅ Task yaratildi!",
            cancellationToken: cancellationToken);
        
        await botClient.SendMessage(
            chatId: callbackQuery.From.Id,
            text: $"✅ <b>Task muvaffaqiyatli yaratildi!</b>\n\n" +
                  $"📋 Task nomi: <b>{session.TaskName}</b>\n" +
                  $"📱 Guruh ID: <code>{session.TelegramGroupId}</code>\n" +
                  $"🔔 Avtomatik xabar: <b>{(session.SendToGroup == true ? "Yoqilgan ✅" : "O'chirilgan ❌")}</b>",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
            cancellationToken: cancellationToken);
    }
}