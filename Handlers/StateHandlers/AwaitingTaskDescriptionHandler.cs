using bot.Models;
using bot.Sercvices;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers.StateHandlers;

public class AwaitingTaskDescriptionHandler(SessionService service) : IStateHandler
{
    public string State => BotStates.AwaitingTaskDescription;

    public async Task HandleAsync(ITelegramBotClient bot, Message msg, UserSession session, CancellationToken ct)
    {
        if (msg.Text != "/skip" && !string.IsNullOrWhiteSpace(msg.Text))
        {
            session.TaskDescription = msg.Text.Trim();
        }

        session.CurrentState = BotStates.AwaitingGroupId;
        service.UpdateSession(session);

        await bot.SendMessage(
            chatId: msg.Chat.Id,
            text: "✅ Tavsif saqlandi!\n\n" +
                  "💬 Uchinchi qadam: jamoangiz Telegram guruh ID sini kiriting.\n\n" +
                  "📌 Qanday topish:\n" +
                  "1️⃣ Meni guruhga qo'shing\n" +
                  "2️⃣ Guruhda /getgroupid yuboring\n" +
                  "3️⃣ ID ni nusxalab shu yerga yuboring\n\n" +
                  "⚠️ Guruh bo'lmasa yoki keyinroq qo'shmoqchi bo'lsangiz:\n/skip",
            cancellationToken: ct);
    }
}