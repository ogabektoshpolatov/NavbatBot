using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace bot.Handlers.TaskCommands;

public class GetGroupIdCommandHandler(ILogger<GetGroupIdCommandHandler> logger) : ICommandHandler
{
    public string Command => "/getGroupId";
    public async Task HandleAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        if (message.Chat.Type == ChatType.Private)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "⚠️ Bu buyruq faqat <b>guruhda</b> ishlaydi!\n\n" +
                      "Quyidagilarni qiling:\n" +
                      "1️⃣ Meni guruhga qo'shing\n" +
                      "2️⃣ Guruhda <code>/getGroupId</code> buyrug'ini yuboring\n" +
                      "3️⃣ Men sizga guruh ID sini ko'rsataman",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
            return;
        }
        
        logger.LogInformation("Group ID requested in chat: {ChatId} ({ChatTitle})", 
            message.Chat.Id, 
            message.Chat.Title);

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: $"📱 <b>Guruh ma'lumotlari:</b>\n\n" +
                  $"🆔 Group ID: <code>{message.Chat.Id}</code>\n" +
                  $"📋 Guruh nomi: <b>{message.Chat.Title}</b>\n\n" +
                  $"💡 Bu ID ni task yaratishda ishlating!\n" +
                  $"Uni ko'chirish uchun ustiga bosing.",
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }
}
