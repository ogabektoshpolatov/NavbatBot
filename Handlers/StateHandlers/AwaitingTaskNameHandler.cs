using bot.Data;
using bot.Models;
using bot.Sercvices;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.StateHandlers;

public class AwaitingTaskNameHandler(AppDbContext dbContext, SessionService service) : IStateHandler
{
    public string State => BotStates.AwaitingTaskName;
    public async Task HandleAsync(ITelegramBotClient bot, Message msg, UserSession session, CancellationToken ct)
    {
        session.TaskName = msg.Text;
        
        session.CurrentState = BotStates.AwaitingGroupId;
        service.UpdateSession(session);

        await bot.SendMessage(
            chatId: msg.Chat.Id, 
            text: "📱 Endi Telegram Group ID ni yuboring:\n\n" +
                  "Group ID ni qanday topish:\n" +
                  "1. Botni guruhga qo'shing\n" +
                  "2. Guruhda /start buyrug'ini yuboring\n" +
                  "3. Bot sizga Group ID ni ko'rsatadi", 
            cancellationToken: ct);
    }
}