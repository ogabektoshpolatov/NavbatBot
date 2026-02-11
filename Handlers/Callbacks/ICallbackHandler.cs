using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.Callbacks;

public interface ICallbackHandler
{
    bool CanHandle(string callbackData);
    Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
}