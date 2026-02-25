using bot.Models;
using bot.Sercvices;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.StateHandlers;

public class AwaitingGroupIdHandler(SessionService service) : IStateHandler
{
    public string State => BotStates.AwaitingGroupId;

    public async Task HandleAsync(ITelegramBotClient bot, Message msg, UserSession session, CancellationToken ct)
    {
        if (msg.Text != "/skip")
        {
            if (!long.TryParse(msg.Text, out long groupId))
            {
                await bot.SendMessage(
                    chatId: msg.Chat.Id,
                    text: "❌ Noto'g'ri format! Raqam kiriting.\n\nMasalan: -1001234567890\n\nYoki o'tkazib yuborish uchun /skip",
                    cancellationToken: ct);
                return;
            }
            session.TelegramGroupId = groupId;
        }

        session.CurrentState = BotStates.AwaitingNotifyTime;
        service.UpdateSession(session);

        await bot.SendMessage(
            chatId: msg.Chat.Id,
            text: "🕐 Bildirishnoma yuborish vaqtini tanlang:",
            replyMarkup: BotKeyboards.NotifyTimeSelectorCreate(),
            cancellationToken: ct);
    }
}