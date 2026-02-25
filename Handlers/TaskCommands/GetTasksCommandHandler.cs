using bot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot.Handlers.TaskCommands;

public class GetTasksCommandHandler(AppDbContext dbContext) : ICommandHandler
{
    public string Command => "/mytasks";

    public async Task HandleAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var userId = message.Chat.Id;

        // Owner bo'lgan tasklar
        var ownedTasks = await dbContext.Tasks
            .Where(t => t.CreatedUserId == userId && t.IsActive)
            .ToListAsync(cancellationToken);

        // Member bo'lgan tasklar
        var memberTasks = await dbContext.Tasks
            .Where(t => t.TaskUsers.Any(tu => tu.UserId == userId && tu.IsActive)
                        && t.CreatedUserId != userId
                        && t.IsActive)
            .ToListAsync(cancellationToken);

        if (!ownedTasks.Any() && !memberTasks.Any())
        {
            await botClient.SendMessage(
                chatId: userId,
                text: "📋 Sizda hozircha tasklar mavjud emas.\n\n➕ Yangi task yarating!",
                cancellationToken: cancellationToken);
            return;
        }

        var buttons = new List<InlineKeyboardButton[]>();

        if (ownedTasks.Any())
        {
            foreach (var t in ownedTasks)
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"👑 {t.Name}",
                        $"task:{t.Id}")
                });
            }
        }

        if (memberTasks.Any())
        {
            foreach (var t in memberTasks)
            {
                var myPosition = await dbContext.TaskUsers
                    .Where(tu => tu.TaskId == t.Id && tu.UserId == userId)
                    .Select(tu => tu.QueuePosition)
                    .FirstOrDefaultAsync(cancellationToken);

                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"👤 {t.Name} — {myPosition}-navbat",
                        $"member_task:{t.Id}")
                });
            }
        }

        await botClient.SendMessage(
            chatId: userId,
            text: "📋 *Mening tasklarim:*\n\n👑 — siz yaratgan\n👤 — siz a'zo",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: cancellationToken);
    }
}