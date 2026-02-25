using bot.Models;
using bot.Sercvices;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.StateHandlers;

public class AwaitingTaskDescriptionHandler(SessionService service) : IStateHandler
{
    public string State => BotStates.AwaitingTaskDescription;

    public async Task HandleAsync(ITelegramBotClient bot, Message msg, UserSession session, CancellationToken ct)
    {
        if (msg.Text != "/skip" && !string.IsNullOrWhiteSpace(msg.Text))
        {
            session.TaskDescription = msg.Text.Trim();
        }

        session.CurrentState = BotStates.AwaitingGroupId;
        service.UpdateSession(session);

        await bot.SendMessage(
            chatId: msg.Chat.Id,
            text: "💬 Telegram Guruh ID ni yuboring:\n\n" +
                  "📌 Qanday topish:\n" +
                  "1. Botni guruhga qo'shing\n" +
                  "2. Guruhda /getgroupid yuboring\n" +
                  "3. Bot ID ni ko'rsatadi\n\n" +
                  "(Keyinroq sozlamoqchi bo'lsangiz /skip yozing)",
            cancellationToken: ct);
    }
}