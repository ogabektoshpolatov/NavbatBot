using bot.Data;
using bot.Models;
using bot.Sercvices;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.Callbacks;

public class AddUserCallbackHandler(AppDbContext dbContext, TaskUiRenderer taskUiRenderer) : ICallbackHandler
{
    public bool CanHandle(string callbackData)
    {
        var parts = callbackData.Split(':');
        return parts.Length == 3 && parts[0] == "task" && parts[2] == "addUser";
    }

    public async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var taskId = int.Parse(callbackQuery!.Data!.Split(':')[1]);
        
        var availableUsers = await dbContext.Users
            .Where(u => !dbContext.TaskUsers
                .Any(tu => tu.TaskId == taskId && tu.UserId == u.UserId && tu.IsActive))
            .ToListAsync(cancellationToken);
        
        await taskUiRenderer.RenderTaskWithUsersAsync(
            botClient,
            callbackQuery,
            taskId,
            BotKeyboards.AddUserList(taskId, availableUsers),
            cancellationToken);
    }
}