using bot.Models;
using bot.Sercvices;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.StateHandlers;

public class AwaitingNotifyIntervalHandler(SessionService service) : IStateHandler
{
    public string State => BotStates.AwaitingNotifyInterval;

    public async Task HandleAsync(ITelegramBotClient bot, Message msg, UserSession session, CancellationToken ct)
    {
        await bot.SendMessage(
            chatId: msg.Chat.Id,
            text: "⚠️ Iltimos, yuqoridagi tugmalardan intervalni tanlang!",
            cancellationToken: ct);
    }
}