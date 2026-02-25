using bot.Data;
using bot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.Callbacks;

public class AssignUserToQueueHandler(AppDbContext dbContext) : ICallbackHandler
{
    public bool CanHandle(string callbackData)
    {
        var parts = callbackData.Split(':');
        return parts.Length == 3 && parts[0] == "task" && parts[2] == "assignUserToQueue";
    }

    public async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var taskId = int.Parse(callbackQuery!.Data!.Split(':')[1]);
        var userId = callbackQuery.From.Id;

        var task = await dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task is null || task.CreatedUserId != userId)
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
        var queueUser = taskUsers.FirstOrDefault(tu => tu.IsCurrent);

        var intervalText = task.NotifyIntervalDays switch
        {
            1 => "Har kun",
            3 => "Har 3 kun",
            7 => "Har hafta",
            _ => $"Har {task.NotifyIntervalDays} kun"
        };

        if (queueUser != null)
        {
            await botClient.EditMessageText(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: $"📌 *{task.Name}*\n" +
                      $"👥 A'zolar: {taskUsers.Count}\n" +
                      $"📅 Interval: {intervalText}\n\n" +
                      $"👤 Hozirgi navbatchi: *{queueUser.User.FirstName}*\n\n" +
                      $"⚠️ Navbatchi allaqachon tayinlangan!",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: BotKeyboards.BackToTask(taskId),
                cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.EditMessageText(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: $"📌 *{task.Name}*\n" +
                      $"👥 A'zolar: {taskUsers.Count}\n" +
                      $"📅 Interval: {intervalText}\n\n" +
                      $"👤 Hozircha navbatchi yo'q\n\n" +
                      $"Navbatchini tanlang:",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: BotKeyboards.ViewUserList(taskId, "assignUserToQueue", users),
                cancellationToken: cancellationToken);
        }
    }
}

public class AssignUserToQueueConfirmHandler(AppDbContext dbContext) : ICallbackHandler
{
    public bool CanHandle(string callbackData)
    {
        var parts = callbackData.Split(':');
        return parts.Length == 5 && parts[0] == "task" && parts[2] == "assignUserToQueue" && parts[4] == "confirm";
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

        var taskUsers = await dbContext.TaskUsers
            .Where(t => t.TaskId == taskId)
            .Include(tu => tu.User)
            .ToListAsync(cancellationToken);

        if (!taskUsers.Any())
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "⚠️ Taskda userlar yo'q!", cancellationToken: cancellationToken);
            return;
        }

        var queueUser = taskUsers.FirstOrDefault(tu => tu.IsCurrent);
        if (queueUser is not null)
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "⚠️ Navbatchi allaqachon tayinlangan!", cancellationToken: cancellationToken);
            await botClient.EditMessageText(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: $"📌 *{task.Name}*\n\n" +
                      $"👤 Navbatchi: *{queueUser.User.FirstName}*",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: BotKeyboards.BackToTask(taskId),
                cancellationToken: cancellationToken);
            return;
        }

        var taskUser = taskUsers.FirstOrDefault(tu => tu.UserId == userId);
        if (taskUser is null)
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, "❌ User topilmadi!", cancellationToken: cancellationToken);
            return;
        }

        taskUser.IsCurrent = true;
        taskUser.UserQueueTime = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await botClient.AnswerCallbackQuery(callbackQuery.Id, "✅ Navbatchi tayinlandi!", cancellationToken: cancellationToken);
        await botClient.EditMessageText(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: $"📌 *{task.Name}*\n\n" +
                  $"✅ Yangi navbatchi: *{taskUser.User.FirstName}*",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.BackToTask(taskId),
            cancellationToken: cancellationToken);
    }
}