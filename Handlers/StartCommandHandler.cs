using bot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot.Handlers;

public class StartCommandHandler(ILogger<StartCommandHandler> logger, AppDbContext dbContext) : ICommandHandler
{
    public string Command => "/start";
    public async Task HandleAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        logger.LogInformation($"User {message.From?.Username} started bot");
        
        var dbUser = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == message.Chat.Id, cancellationToken);
        if (dbUser is null)
        {
            var user = new Entities.User()
            {
                UserId = message.Chat.Id,
                Username = message.From?.Username,
                FirstName = message.From?.FirstName,
                IsActive = true,
            };
            
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            Console.WriteLine($"User {user.UserId} successfully saved");
        }

        Console.WriteLine("StartCommandHandler.HandleAsync");
        await botClient.SetMyCommands(new[]
        {
            new BotCommand { Command = "start", Description = "Open main menu" },
            new BotCommand { Command = "getGroupId", Description = "Guruh ID sini olish (faqat guruhda)" },
        }, scope:null, languageCode:null, cancellationToken: cancellationToken);
        
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
              InlineKeyboardButton.WithCallbackData("\u2b05\ufe0f Left", "button_left"),
              InlineKeyboardButton.WithCallbackData("\u2b05\ufe0f Right", "button_right"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("ℹ️ Info", "button_info")
            }
        });
        
        var replykeyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "➕ Create task", "📋 My Tasks" },
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await botClient.SendMessage(
            chatId:message.Chat.Id,
            text:"\ud83d\ude0a Salom! Botga xush kelibsiz!",
            replyMarkup:replykeyboard,
            cancellationToken:cancellationToken);
    }
}