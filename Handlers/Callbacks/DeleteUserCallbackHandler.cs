using bot.Data;
using bot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.Callbacks;

public class DeleteUserCallbackHandler(AppDbContext dbContext) : ICallbackHandler
{
    public bool CanHandle(string callbackData)
    {
        var parts = callbackData.Split(':');
        return parts.Length == 3 && parts[0] == "task" && parts[2] == "removeUser";
    }

    public async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var taskId = int.Parse(callbackQuery!.Data!.Split(':')[1]);
        var callerId = callbackQuery.From.Id;

        var task = await dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task is null || task.CreatedUserId != callerId)
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "⛔ Ruxsat yo'q!", showAlert: true, cancellationToken: cancellationToken);
            return;
        }

        var taskUsers = await dbContext.TaskUsers
            .Where(tu => tu.TaskId == taskId && tu.IsActive)
            .OrderBy(tu => tu.QueuePosition)
            .Include(tu => tu.User)
            .ToListAsync(cancellationToken);

        var users = taskUsers.Select(tu => tu.User).ToList();

        await botClient.EditMessageText(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: $"📌 *{task.Name}*\n" +
                  $"👥 A'zolar: {taskUsers.Count}\n\n" +
                  $"O'chirmoqchi bo'lgan a'zoni tanlang:",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.ViewUserList(taskId, "removeUser", users),
            cancellationToken: cancellationToken);
    }
}

public class DeleteUserConfirmCallbackHandler(AppDbContext dbContext) : ICallbackHandler
{
    public bool CanHandle(string callbackData)
    {
        var parts = callbackData.Split(':');
        return parts.Length == 5 && parts[0] == "task" && parts[2] == "removeUser" && parts[4] == "confirm";
    }

    public async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var taskId = int.Parse(callbackQuery!.Data!.Split(':')[1]);
        var userId = long.Parse(callbackQuery!.Data!.Split(':')[3]);
        var callerId = callbackQuery.From.Id;

        var task = await dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task is null || task.CreatedUserId != callerId)
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "⛔ Ruxsat yo'q!", showAlert: true, cancellationToken: cancellationToken);
            return;
        }

        var taskUser = await dbContext.TaskUsers
            .FirstOrDefaultAsync(tu => tu.TaskId == taskId && tu.UserId == userId && tu.IsActive, cancellationToken);

        if (taskUser != null)
        {
            if (taskUser.IsCurrent)
            {
                var nextTaskUser = await dbContext.TaskUsers
                    .Where(tu => tu.TaskId == taskId && tu.IsActive && tu.QueuePosition > taskUser.QueuePosition)
                    .OrderBy(tu => tu.QueuePosition)
                    .FirstOrDefaultAsync(cancellationToken);

                if (nextTaskUser is null)
                {
                    nextTaskUser = await dbContext.TaskUsers
                        .Where(tu => tu.TaskId == taskId && tu.IsActive && tu.Id != taskUser.Id)
                        .OrderBy(tu => tu.QueuePosition)
                        .FirstOrDefaultAsync(cancellationToken);
                }

                if (nextTaskUser != null)
                {
                    nextTaskUser.IsCurrent = true;
                    nextTaskUser.UserQueueTime = DateTime.UtcNow;
                }
            }

            dbContext.Remove(taskUser);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var remainingUsers = await dbContext.Users
            .Where(u => dbContext.TaskUsers
                .Any(tu => tu.TaskId == taskId && tu.UserId == u.UserId && tu.IsActive))
            .ToListAsync(cancellationToken);

        await botClient.AnswerCallbackQuery(
            callbackQuery.Id,
            "✅ Foydalanuvchi o'chirildi.",
            cancellationToken: cancellationToken);

        await botClient.EditMessageText(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: $"📌 *{task.Name}*\n" +
                  $"👥 A'zolar: {remainingUsers.Count}\n\n" +
                  $"O'chirmoqchi bo'lgan a'zoni tanlang:",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.ViewUserList(taskId, "removeUser", remainingUsers),
            cancellationToken: cancellationToken);
    }
}