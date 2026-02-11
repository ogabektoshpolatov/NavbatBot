using bot.Data;
using bot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.Callbacks;

public class TaskMenuCallbackHandler(AppDbContext dbContext) : ICallbackHandler
{
    public bool CanHandle(string callbackData)  
    {
        var parts = callbackData.Split(':');
        return parts.Length == 2 && parts[0] == "task";
    }

    public async Task HandleAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var taskId  = int.Parse(callbackQuery!.Data!.Split(':')[1]);
        
        var task = await dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is not null)
        {
            var userCount = await dbContext.TaskUsers.CountAsync(tu => tu.TaskId == taskId && tu.IsActive, cancellationToken);

            await botClient.EditMessageText(
                chatId:      callbackQuery.Message!.Chat.Id,
                messageId:   callbackQuery.Message.MessageId,
                text:        $"📌 *{task.Name}*\n👥 Userlar soni: {userCount}\n⏰ Vaqt: {task.ScheduleTime.ToString("dd.MM.yyyy HH:mm")}",
                parseMode:   Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: BotKeyboards.TaskMenu(taskId),
                cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.AnswerCallbackQuery(
                callbackQuery.Id, 
                text:"Hammasi Ok TaskMenuga kirdi",
                cancellationToken: cancellationToken);
        }
    }
}