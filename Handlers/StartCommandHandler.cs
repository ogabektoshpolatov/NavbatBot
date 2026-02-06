using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot.Handlers;

public class StartCommandHandler(ILogger<StartCommandHandler> logger) : ICommandHandler
{
    public string Command => "/start";
    public async Task HandleAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        logger.LogInformation($"User {message.From?.Username} started bot");

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

        await botClient.SendMessage(
            chatId:message.Chat.Id,
            text:"\ud83d\ude0a Salom! Botga xush kelibsiz!",
            replyMarkup: keyboard,
            cancellationToken:cancellationToken);
    }
}