using bot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot.Handlers.TaskCommands;

public class GetTasksCommandHandler(AppDbContext dbContext) : ICommandHandler
{
    public string Command => "/mytasks";

    public async Task HandleAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var userId = message.Chat.Id;

        var ownedTasks = await dbContext.Tasks
            .Where(t => t.CreatedUserId == userId && t.IsActive)
            .Include(t => t.TaskUsers.Where(tu => tu.IsActive))
            .ToListAsync(cancellationToken);

        var memberTasks = await dbContext.Tasks
            .Where(t => t.TaskUsers.Any(tu => tu.UserId == userId && tu.IsActive)
                        && t.CreatedUserId != userId
                        && t.IsActive)
            .Include(t => t.TaskUsers.Where(tu => tu.IsActive))
            .ToListAsync(cancellationToken);

        if (!ownedTasks.Any() && !memberTasks.Any())
        {
            await botClient.SendMessage(
                chatId: userId,
                text: "🗂 *Navbatlar*\n\n" +
                      "Hozircha hech qanday navbat yo'q.\n\n" +
                      "➕ Yangi navbat yaratish uchun tugmani bosing:",
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("➕ Yangi navbat yaratish", "create_task") }
                }),
                cancellationToken: cancellationToken);
            return;
        }

        var buttons = new List<InlineKeyboardButton[]>();
        
        if (ownedTasks.Any())
        {
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("👑 Mening navbatlarim", "noop") });

            foreach (var t in ownedTasks)
            {
                var memberCount = t.TaskUsers.Count;
                var currentUser = t.TaskUsers.FirstOrDefault(tu => tu.IsCurrent);
                var statusIcon = currentUser != null ? "🟢" : "⚪️";

                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"{statusIcon} {t.Name}  •  👥{memberCount}",
                        $"task:{t.Id}")
                });
            }
        }
        
        if (memberTasks.Any())
        {
            if (ownedTasks.Any())
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄", "noop") });

            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🚶 Qo'shilgan navbatlar", "noop") });

            foreach (var t in memberTasks)
            {
                var myTaskUser = t.TaskUsers.FirstOrDefault(tu => tu.UserId == userId);
                var totalMembers = t.TaskUsers.Count;
                var isCurrent = myTaskUser?.IsCurrent == true;
                var isPending = myTaskUser?.IsPendingConfirmation == true;

                string statusIcon;
                string positionText;

                if (isCurrent)
                {
                    statusIcon = "🟢";
                    positionText = "Navbatdasiz!";
                }
                else if (isPending)
                {
                    statusIcon = "⏳";
                    positionText = "Tasdiq kutilmoqda";
                }
                else
                {
                    statusIcon = "🔵";
                    positionText = $"{myTaskUser?.QueuePosition}-o'rin / {totalMembers}";
                }

                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"{statusIcon} {t.Name}  •  {positionText}",
                        $"member_task:{t.Id}")
                });
            }
        }
        
        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄", "noop") });
        
        var totalOwned = ownedTasks.Count;
        var totalMember = memberTasks.Count;
        // var activeCount = ownedTasks.Count(t => t.TaskUsers.Any(tu => tu.IsCurrent))
        //                 + memberTasks.Count(t => t.TaskUsers.Any(tu => tu.UserId == userId && tu.IsCurrent));

        var headerText =
            $"🗂 *Mening navbatlarim*\n\n" +
            $"👑 Siz yaratgan navbatlar: *{totalOwned} ta*\n" +
            $"🚶 Siz qo'shilgan navbatlar: *{totalMember} ta*\n" +
            // (activeCount > 0 ? $"🟢 Hozir navbatdasiz: *{activeCount} ta*\n" : "") +
            $"\n_Navbatni boshqarish uchun tanlang:_";

        await botClient.SendMessage(
            chatId: userId,
            text: headerText,
            parseMode: ParseMode.Markdown,
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: cancellationToken);
    }
}