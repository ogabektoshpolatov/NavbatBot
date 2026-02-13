using bot.Handlers.Callbacks;
using bot.Models;
using bot.Sercvices;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.StateHandlers;

public class AwaitingAutoNotifyHandler(SessionService sessionService) : IStateHandler
{
    public string State => BotStates.AwaitingAutoNotify;
    public async Task HandleAsync(ITelegramBotClient bot, Message msg, UserSession session, CancellationToken ct)
    {
        await bot.SendMessage(
            chatId: msg.Chat.Id,
            text: "⚠️ Iltimos, yuqoridagi tugmalardan birini tanlang!",
            cancellationToken: ct);
    }
}