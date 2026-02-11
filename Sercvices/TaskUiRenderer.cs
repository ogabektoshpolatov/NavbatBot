using bot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot.Sercvices;

public class TaskUiRenderer(AppDbContext dbContext)
{
    public async Task RenderTaskWithUsersAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        int taskId,
        InlineKeyboardMarkup keyboard,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        var userCount = await dbContext.TaskUsers
            .CountAsync(tu => tu.TaskId == taskId && tu.IsActive, cancellationToken);

        await botClient.EditMessageText(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text:
            $"""
             📌 *{task.Name}*
             👥 Userlar soni: {userCount}
             ⏰ Vaqt: {task.ScheduleTime:dd.MM.yyyy HH:mm}
             """,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }
}