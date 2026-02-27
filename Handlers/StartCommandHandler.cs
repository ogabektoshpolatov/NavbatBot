using bot.Data;
using bot.Models;
using bot.Sercvices;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace bot.Handlers;

public class StartCommandHandler(
    ILogger<StartCommandHandler> logger,
    AppDbContext dbContext,
    SessionService sessionService) : ICommandHandler
{
    public string Command => "/start";

    public async Task HandleAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var userId = message.Chat.Id;
        var text = message.Text ?? "";

        // User saqlash / yangilash
        var dbUser = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (dbUser is null)
        {
            dbContext.Users.Add(new Entities.User
            {
                UserId = userId,
                Username = message.From?.Username,
                FirstName = message.From?.FirstName,
                LastName = message.From?.LastName,
                LanguageCode = message.From?.LanguageCode,
                IsActive = true
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Yangi user: {UserId}", userId);
        }
        else
        {
            dbUser.Username = message.From?.Username;
            dbUser.FirstName = message.From?.FirstName;
            dbUser.LastName = message.From?.LastName;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Invite link orqali kelgan: /start join_TOKEN
        if (text.StartsWith("/start join_"))
        {
            var token = text.Replace("/start join_", "").Trim();
            await HandleJoinAsync(botClient, message, userId, token, cancellationToken);
            return;
        }

        // Bot commandlarini set qilish
        await botClient.SetMyCommands(
            new[]
            {
                new BotCommand { Command = "start", Description = "Asosiy menyu" },
            },
            scope: new Telegram.Bot.Types.BotCommandScopeAllPrivateChats(),
            cancellationToken: cancellationToken);

        await botClient.SetMyCommands(
            new[]
            {
                new BotCommand { Command = "getgroupid", Description = "Guruh ID sini olish" },
            },
            scope: new Telegram.Bot.Types.BotCommandScopeAllGroupChats(),
            cancellationToken: cancellationToken);

        await botClient.SendMessage(
            chatId: userId,
            text: "👋 Salom! Navbat boshqaruv botiga xush kelibsiz!\n\n" +
                  "📌 Quyidagilardan birini tanlang:",
            replyMarkup: BotKeyboards.MainMenu(),
            cancellationToken: cancellationToken);
    }

    private async Task HandleJoinAsync(
        ITelegramBotClient botClient,
        Message message,
        long userId,
        string token,
        CancellationToken ct)
    {
        var task = await dbContext.Tasks
            .Include(t => t.TaskUsers)
            .FirstOrDefaultAsync(t => t.InviteToken == token, ct);
        
        if (task is null)
        {
            await botClient.SendMessage(
                chatId: userId,
                text: "👋 Salom, {message.From?.FirstName}!\n\n" +
                      "🤖 Bu bot navbatchilikni boshqarish uchun:\n\n" +
                      "➕ Task yarating → do'stlaringizni invite qiling\n" +
                      "🔄 Bot avtomatik navbatni boshqaradi\n" +
                      "🔔 Har kuni belgilangan vaqtda xabar yuboradi\n\n" +
                      "👇 Boshlash uchun tanlang:",
                replyMarkup: BotKeyboards.MainMenu(),
                cancellationToken: ct);
            return;
        }

        if (!task.InviteIsActive)
        {
            await botClient.SendMessage(
                chatId: userId,
                text: "🔒 Bu invite link yopilgan. Iltimos, topshiriq egasiga murojat qiling.",
                replyMarkup: BotKeyboards.MainMenu(),
                cancellationToken: ct);
            return;
        }

        var activeMembers = task.TaskUsers.Count(tu => tu.IsActive);

        if (activeMembers >= task.MaxMembers)
        {
            await botClient.SendMessage(
                chatId: userId,
                text: "❌ Navbat to'liq. Yangi a'zo qo'shib bo'lmaydi.",
                replyMarkup: BotKeyboards.MainMenu(),
                cancellationToken: ct);
            return;
        }

        var alreadyMember = task.TaskUsers.Any(tu => tu.UserId == userId && tu.IsActive);
        if (alreadyMember)
        {
            await botClient.SendMessage(
                chatId: userId,
                text: $"ℹ️ Siz allaqachon *{task.Name}* navbatiga qo'shilgansiz!",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: BotKeyboards.MainMenu(),
                cancellationToken: ct);
            return;
        }

        // Owner bo'lsa
        if (task.CreatedUserId == userId)
        {
            await botClient.SendMessage(
                chatId: userId,
                text: "ℹ️ Bu sizning o'z taskingiz!",
                replyMarkup: BotKeyboards.MainMenu(),
                cancellationToken: ct);
            return;
        }

        var owner = await dbContext.Users
            .FirstOrDefaultAsync(u => u.UserId == task.CreatedUserId, ct);

        var intervalText = task.NotifyIntervalDays switch
        {
            1 => "Har kun",
            3 => "Har 3 kun",
            7 => "Har hafta",
            _ => $"Har {task.NotifyIntervalDays} kun"
        };

        // Session ga taskId saqlash
        var session = sessionService.GetOrCreateSession(userId);
        session.EditingTaskId = task.Id;
        sessionService.UpdateSession(session);

        await botClient.SendMessage(
            chatId: userId,
            text: $"📋 *{task.Name}* navbatiga qo'shilmoqchimisiz?\n\n" +
                  $"👤 Yaratgan: {owner?.FirstName ?? "Noma'lum"}\n" +
                  $"👥 A'zolar: {activeMembers}/{task.MaxMembers}\n" +
                  $"📅 Interval: {intervalText}\n" +
                  $"🕐 Vaqt: {task.NotifyTime:hh\\:mm}",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: BotKeyboards.JoinButtons(task.Id),
            cancellationToken: ct);
    }
}