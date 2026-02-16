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
        
        var task = await dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        
        var taskUsers = await dbContext.TaskUsers
            .Where(tu => tu.TaskId == taskId && tu.IsActive)
            .OrderBy(tu => tu.QueuePosition) 
            .Include(tu => tu.User)
            .ToListAsync(cancellationToken);

        var users = taskUsers.Select(tu => tu.User).ToList();
        
        var queueUser = taskUsers.FirstOrDefault(tu => tu.IsCurrent);
        
        var currentQueueText = queueUser != null
            ? $"👤 Hozirgi navbatchi: *{queueUser.User.FirstName}*"
            : "👤 Hozircha navbatchi yo‘q";
        
        if (queueUser != null)
        {
            await botClient.EditMessageText(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text:
                $"""
                 📌 *{task.Name}*
                 👥 Userlar soni: {taskUsers.Count()}
                 ⏰ Vaqt: {task.ScheduleTime:dd.MM.yyyy HH:mm}
                 👤 {currentQueueText}
                 """,
                replyMarkup: BotKeyboards.BackToTask(taskId),
                cancellationToken: cancellationToken
            );
        }
        else
        {
            await botClient.EditMessageText(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text:
                $"""
                 📌 *{task.Name}*
                 👥 Userlar soni: {taskUsers.Count()}
                 ⏰ Vaqt: {task.ScheduleTime:dd.MM.yyyy HH:mm}
                 👤 {currentQueueText}
                 ! Navbatchini tanlang.
                 """,
                replyMarkup: BotKeyboards.ViewUserList(taskId, "assignUserToQueue", users),
                cancellationToken: cancellationToken
            );
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
        
        var task = await dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        var taskUsers = await dbContext.TaskUsers.Where(t => t.TaskId == taskId)
            .ToListAsync(cancellationToken);

        if (!taskUsers.Any())
        {
         await botClient.AnswerCallbackQuery(
             callbackQuery.Id,
             text:
             $"""
              Taskda userlar mavjud
              """,
             cancellationToken: cancellationToken
         );
        }

        var queueuser = taskUsers.FirstOrDefault(tu => tu.IsCurrent);

        if (queueuser is not null)
        {
            await botClient.AnswerCallbackQuery(
                callbackQuery.Id,
                text:
                $"""
                 ✅ Bu taskda navbatchi tayinlangan..
                 """,
                cancellationToken: cancellationToken
            );
            
            await botClient.EditMessageText(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text:
                $"""
                 📌 *{task.Name}*
                 👥 Navbatchi: {queueuser.User.FirstName}
                 ⏰ Vaqt: {task.ScheduleTime:dd.MM.yyyy HH:mm}
                 """,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: BotKeyboards.BackToTask(taskId),
                cancellationToken: cancellationToken
            );
        }
        
        var taskUser = taskUsers.FirstOrDefault(tu => tu.UserId == userId);

        if (taskUser != null)
        {
            taskUser.IsCurrent = true;
            taskUser.UserQueueTime = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            
            await botClient.EditMessageText(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text:
                $"""
                 📌 *{task.Name}*
                 👥 Navbatchi: {taskUser.User.FirstName}
                 ⏰ Vaqt: {task.ScheduleTime:dd.MM.yyyy HH:mm}
                 """,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: BotKeyboards.BackToTask(taskId),
                cancellationToken: cancellationToken
            );
        }
    }
}