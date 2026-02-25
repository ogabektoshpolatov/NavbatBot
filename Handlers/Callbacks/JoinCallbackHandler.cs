using bot.Data;
using bot.Sercvices;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using bot.Models;

namespace bot.Handlers.Callbacks;

public class JoinConfirmCallbackHandler(
    AppDbContext db,
    SessionService sessionService,
    ILogger<JoinConfirmCallbackHandler> logger) : ICallbackHandler
{
    public bool CanHandle(string data)
    {
        var parts = data.Split(':');
        return parts.Length == 3 && parts[0] == "join" && parts[2] == "confirm";
    }

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery cq, CancellationToken ct)
    {
        var taskId = int.Parse(cq.Data!.Split(':')[1]);
        var userId = cq.From.Id;

        var task = await db.Tasks
            .Include(t => t.TaskUsers)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct);

        if (task is null)
        {
            await bot.AnswerCallbackQuery(cq.Id, "❌ Task topilmadi.", showAlert: true, cancellationToken: ct);
            return;
        }

        if (!task.InviteIsActive)
        {
            await bot.AnswerCallbackQuery(cq.Id, "🔒 Invite link yopilgan.", showAlert: true, cancellationToken: ct);
            return;
        }

        var activeMembers = task.TaskUsers.Count(tu => tu.IsActive);
        if (activeMembers >= task.MaxMembers)
        {
            await bot.AnswerCallbackQuery(cq.Id, "❌ Navbat to'liq!", showAlert: true, cancellationToken: ct);
            return;
        }

        var alreadyMember = task.TaskUsers.Any(tu => tu.UserId == userId && tu.IsActive);
        if (alreadyMember)
        {
            await bot.AnswerCallbackQuery(cq.Id, "ℹ️ Siz allaqachon qo'shilgansiz!", cancellationToken: ct);
            return;
        }

        var maxPos = task.TaskUsers.Any() ? task.TaskUsers.Max(tu => tu.QueuePosition) : 0;

        db.TaskUsers.Add(new bot.Entities.TaskUser
        {
            TaskId = taskId,
            UserId = userId,
            QueuePosition = maxPos + 1,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);

        // Session tozalash
        var session = sessionService.GetOrCreateSession(userId);
        session.EditingTaskId = null;
        sessionService.UpdateSession(session);

        logger.LogInformation("User {UserId} joined task {TaskId}", userId, taskId);

        // Owner ga xabar
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
        try
        {
            await bot.SendMessage(
                chatId: task.CreatedUserId,
                text: $"🔔 *Yangi a'zo!*\n\n" +
                      $"👤 {user?.FirstName ?? cq.From.FirstName} sizning *{task.Name}* navbatingizga qo'shildi!\n" +
                      $"📍 Pozitsiya: {maxPos + 1}",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: ct);
        }
        catch { /* Owner bot bilan gaplashmagan bo'lishi mumkin */ }

        await bot.AnswerCallbackQuery(cq.Id, "✅ Muvaffaqiyatli qo'shildingiz!", cancellationToken: ct);

        await bot.EditMessageText(
            chatId: cq.Message!.Chat.Id,
            messageId: cq.Message.MessageId,
            text: $"✅ *{task.Name}* navbatiga qo'shildingiz!\n\n" +
                  $"📍 Sizning pozitsiyangiz: *{maxPos + 1}*\n\n" +
                  $"📋 Mening tasklarimni ko'rish uchun: /mytasks",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            cancellationToken: ct);
    }
}

public class JoinCancelCallbackHandler(SessionService sessionService) : ICallbackHandler
{
    public bool CanHandle(string data)
    {
        var parts = data.Split(':');
        return parts.Length == 3 && parts[0] == "join" && parts[2] == "cancel";
    }

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery cq, CancellationToken ct)
    {
        var session = sessionService.GetOrCreateSession(cq.From.Id);
        session.EditingTaskId = null;
        sessionService.UpdateSession(session);

        await bot.AnswerCallbackQuery(cq.Id, cancellationToken: ct);
        await bot.EditMessageText(
            chatId: cq.Message!.Chat.Id,
            messageId: cq.Message.MessageId,
            text: "❌ Qo'shilish bekor qilindi.",
            cancellationToken: ct);
    }
}