using bot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.Callbacks;

public class SkipQueueCallbackHandler(AppDbContext dbContext) : ICallbackHandler
{
    public bool CanHandle(string callbackData)
    {
        var parts = callbackData.Split(':');
        return parts.Length == 3 && parts[0] == "task" && parts[2] == "skipQueue";
    }

    public async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var taskId = int.Parse(callbackQuery.Data!.Split(':')[1]);
        var userId = callbackQuery.From.Id;

        var task = await dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (task is null || task.CreatedUserId != userId)
        {
            await botClient.AnswerCallbackQuery(
                callbackQuery.Id, "⛔ Ruxsat yo'q!", showAlert: true, cancellationToken: ct);
            return;
        }

        var taskUsers = await dbContext.TaskUsers
            .Where(x => x.TaskId == taskId && x.IsActive)
            .OrderBy(x => x.QueuePosition)
            .ToListAsync(ct);

        var currentQueueUser = taskUsers.FirstOrDefault(x => x.IsCurrent);

        if (currentQueueUser is null)
        {
            await botClient.AnswerCallbackQuery(
                callbackQuery.Id, "\ud83d\udc64 Hali hech kim navbatchi etib tayinlanmagan", cancellationToken: ct);
            return;
        }

        var nextTaskUser = taskUsers
            .Where(tu => tu.QueuePosition > currentQueueUser.QueuePosition)
            .OrderBy(tu => tu.QueuePosition)
            .FirstOrDefault()
            ?? taskUsers.OrderBy(tu => tu.QueuePosition).FirstOrDefault();

        currentQueueUser.IsCurrent = false;
        currentQueueUser.UserQueueTime = null;

        if (nextTaskUser != null && nextTaskUser.Id != currentQueueUser.Id)
        {
            nextTaskUser.IsCurrent = true;
            nextTaskUser.UserQueueTime = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(ct);
        await botClient.AnswerCallbackQuery(callbackQuery.Id, "✅ Navbat o'tkazildi!", cancellationToken: ct);
    }
}