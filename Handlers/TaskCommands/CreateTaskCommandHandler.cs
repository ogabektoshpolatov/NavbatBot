using bot.Models;
using bot.Sercvices;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.TaskCommands;

public class CreateTaskCommandHandler(SessionService sessionService) : ICommandHandler
{
    public string Command => "/createtask";

    public async Task HandleAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var session = sessionService.GetOrCreateSession(message.Chat.Id);
        session.CurrentState = BotStates.AwaitingTaskName;
        session.TaskName = null;
        session.TaskDescription = null;
        session.TelegramGroupId = null;
        session.SendToGroup = null;
        session.NotifyIntervalDays = null;
        session.NotifyTime = null;
        sessionService.UpdateSession(session);

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "📝 Task nomini kiriting:",
            cancellationToken: cancellationToken);
    }
}