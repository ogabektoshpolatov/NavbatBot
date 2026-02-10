using bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.StateHandlers;

public interface IStateHandler
{
    string State { get; }
    Task HandleAsync(ITelegramBotClient bot, Message msg, UserSession session, CancellationToken ct);
}