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
        session.CurrentState = BotStates.AwaitingTaskName;
        service.UpdateSession(session);

        var task = new Entities.Task()
        {
            Name = msg.Text,
            ScheduleTime = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            TelegramGroupId = -4972162716,
            CreatedUserId = session.UserId,
            IsActive = true
        };
        
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync(ct);
        
        await bot.SendMessage(msg.Chat.Id, $"✅ Task '{msg.Text}' saqlandi!", cancellationToken: ct);
    }
}