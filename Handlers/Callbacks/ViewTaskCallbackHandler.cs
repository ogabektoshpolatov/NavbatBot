using bot.Data;
using bot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.Callbacks;

public class ViewTaskCallbackHandler(AppDbContext dbContext) : ICallbackHandler
{
    public bool CanHandle(string callbackData)
    {
        var parts = callbackData.Split(':');
        return parts.Length == 3 && parts[0] == "task" && parts[2] == "viewUsers";
    }

    public async Task HandleAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var taskId = int.Parse(callbackQuery!.Data!.Split(':')[1]);

//         var task = await dbContext.Tasks
//             .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
//
//         // TaskUsers jadvalidan tartib bilan olish
//         var taskUsers = await dbContext.TaskUsers
//             .Where(tu => tu.TaskId == taskId && tu.IsActive)
//             .OrderBy(tu => tu.QueueIndex) // agar QueueIndex bo‘lsa
//             .Select(tu => new { tu.User.FullName, tu.IsCurrent })
//             .ToListAsync(cancellationToken);
//
//         var userCount = taskUsers.Count;
//
//         // Ro‘yxatni tartib bilan chiqarish
//         var userListText = string.Join("\n",
//             taskUsers.Select((u, i) =>
//                 $"{i + 1}. {u.FullName}{(u.IsCurrent ? " ✅" : "")}"));
//
//         await botClient.EditMessageText(
//             chatId: callbackQuery.Message!.Chat.Id,
//             messageId: callbackQuery.Message.MessageId,
//             text:
//             $"""
//              📌 *{task.Name}*
//              👥 Userlar soni: {userCount}
//              ⏰ Vaqt: {task.ScheduleTime:dd.MM.yyyy HH:mm}
//
//              👤 Userlar:
//              {userListText}
//              """,
//             parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
//             replyMarkup: BotKeyboards.ViewUserListWithBack(taskId, "removeUser", taskUsers),
//             cancellationToken: cancellationToken
//         );
    }

}