using bot.Models;
using bot.Sercvices;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot.Handlers.StateHandlers;

public class AwaitingGroupIdHandler(SessionService sessionService) : IStateHandler
{
    public string State => BotStates.AwaitingGroupId;
    public async Task HandleAsync(ITelegramBotClient bot, Message msg, UserSession session, CancellationToken ct)
    {
        if (!long.TryParse(msg.Text, out long groupId))
        {
            await bot.SendMessage(
                chatId: msg.Chat.Id,
                text: "❌ Noto'g'ri format! Iltimos, raqam kiriting.\n\nMasalan: -1001234567890",
                cancellationToken: ct);
            return;
        }
        
        session.TelegramGroupId = groupId;
        session.CurrentState = BotStates.AwaitingAutoNotify;
        sessionService.UpdateSession(session);
        
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Ha, guruhga yuborsin", "notify_yes"),
                InlineKeyboardButton.WithCallbackData("❌ Yo'q, kerak emas", "notify_no")
            }
        });

        await bot.SendMessage(
            chatId: msg.Chat.Id,
            text: "🔔 Har kuni guruhga navbatchi haqida xabar yuborilsinmi?",
            replyMarkup: keyboard,
            cancellationToken: ct);
    }
}