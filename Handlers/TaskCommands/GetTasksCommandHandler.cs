using bot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.TaskCommands;

public class GetTasksCommandHandler(AppDbContext dbContext) : ICommandHandler
{
    public string Command => "/mytasks";
    public async Task HandleAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var dbTasks = await dbContext.Tasks.Where(t => t.CreatedUserId == message.Chat.Id).ToListAsync(cancellationToken);
        
        if (!dbTasks.Any())
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Sizda hozircha tasklar mavjud emas ❌",
                cancellationToken: cancellationToken);
            return;
        }

        var text = "📋 *My Tasks*\n\n";

        for (int i = 0; i < dbTasks.Count; i++)
        {
            text += $"{i + 1}. {dbTasks[i].Name}\n";
        }
        
        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: text,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }
}