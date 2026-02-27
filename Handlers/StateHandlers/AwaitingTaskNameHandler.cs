using bot.Models;
using bot.Sercvices;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.StateHandlers;

public class AwaitingTaskNameHandler(SessionService service) : IStateHandler
{
    public string State => BotStates.AwaitingTaskName;

    public async Task HandleAsync(ITelegramBotClient bot, Message msg, UserSession session, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(msg.Text))
        {
            await bot.SendMessage(msg.Chat.Id, "❌ Iltimos, task nomini kiriting:", cancellationToken: ct);
            return;
        }

        session.TaskName = msg.Text.Trim();
        session.CurrentState = BotStates.AwaitingTaskDescription;
        service.UpdateSession(session);

        await bot.SendMessage(
            chatId: msg.Chat.Id,
            text: "✅ Navbatchilik nomi saqlandi!\n\n" +
                  "📋 Ikkinchi qadam: navbat tavsifini kiriting.\n" +
                  "Masalan: \"Har haftada 1 kishi 1 kun tozalaydi\"\n\n" +
                  "(Ixtiyoriy — o'tkazib yuborish uchun /skip)",
            cancellationToken: ct);
    }
}