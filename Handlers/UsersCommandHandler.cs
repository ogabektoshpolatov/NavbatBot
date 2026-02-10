using bot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot.Handlers;

public class UsersCommandHandler(ILogger<UsersCommandHandler> logger, AppDbContext dbContext) : ICommandHandler
{
    public string Command => "/users";
    public async Task HandleAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var dbUsers = await dbContext.Users
            .Where(u => u.IsActive)
            .ToListAsync(cancellationToken);

        var buttons = dbUsers
            .Select(u => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    text: u.FirstName ?? "Unknown",
                    callbackData: $"{u.UserId}"
                )
            })
            .ToList();

        var keyboard = new InlineKeyboardMarkup(buttons);
        
        await botClient.SendMessage(
            chatId:message.Chat.Id,
            text: "Foydalanuvchilar ro`yxati",
            replyMarkup:keyboard,
            cancellationToken:cancellationToken);
    }
}