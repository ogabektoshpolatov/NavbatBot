using bot.Handlers;
using bot.Handlers.Callbacks;
using bot.Handlers.StateHandlers;
using bot.Models;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace bot.Sercvices;

public class TelegramBotService : BackgroundService
{
    private readonly TelegramBotClient _botClient;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public TelegramBotService(
        IConfiguration configuration,
        ILogger<TelegramBotService> logger,
        IServiceProvider serviceProvider)
    {
        var token = configuration["TelegramBot:Token"]
            ?? throw new NullReferenceException("TelegramBot:Token");
        _botClient = new TelegramBotClient(token);
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _botClient.DeleteWebhook(dropPendingUpdates: true, stoppingToken);

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: new ReceiverOptions { },
            cancellationToken: stoppingToken);

        var me = await _botClient.GetMe(stoppingToken);
        _logger.LogInformation("Bot @{Username} boshlandi!", me.Username);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    if (update.Message is not null)
                        await HandleMessageUpdate(botClient, update.Message, ct);
                    break;
                case UpdateType.CallbackQuery:
                    if (update.CallbackQuery is not null)
                        await HandleCallbackUpdate(botClient, update.CallbackQuery, ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Update handle qilishda xatolik");
        }
    }

    private async Task HandleCallbackUpdate(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var data = callbackQuery.Data;
        if (string.IsNullOrEmpty(data)) return;

        _logger.LogInformation("Callback @{User}: {Data}", callbackQuery.From.Username, data);

        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<ICallbackHandler>();

        var handler = handlers.FirstOrDefault(h => h.CanHandle(data));
        if (handler is not null)
        {
            await handler.HandleAsync(botClient, callbackQuery, ct);
        }
        else
        {
            if (data == "noop") return;
            _logger.LogWarning("Handler topilmadi: {Data}", data);
            await botClient.AnswerCallbackQuery(
                callbackQuery.Id,
                "❓ Noma'lum amal.",
                cancellationToken: ct);
        }
    }

    private async Task HandleMessageUpdate(ITelegramBotClient botClient, Message message, CancellationToken ct)
    {
        if (message.Text is not { } messageText) return;

        // Guruhdan oddiy xabarlarni ignore
        if (message.Chat.Type != ChatType.Private && !messageText.StartsWith('/'))
            return;

        // Menu tugmalarini commandga o'girish
        messageText = NormalizeMenuCommand(messageText);

        // Bot username ni o'chirish
        if (messageText.StartsWith('/'))
            messageText = RemoveBotUsername(messageText);

        message.Text = messageText;

        _logger.LogInformation("Xabar '{Text}' from @{User}", messageText, message.From?.Username);

        using var scope = _serviceProvider.CreateScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<SessionService>();
        var session = sessionService.GetOrCreateSession(message.Chat.Id);

        // Command bo'lsa
        if (messageText.StartsWith('/'))
        {
            // /skip faqat state ichida ishlaydi
            if (messageText == "/skip" && !string.IsNullOrEmpty(session.CurrentState))
            {
                await HandleStateLogic(botClient, message, session, sessionService, scope, ct);
                return;
            }

            sessionService.ClearSession(message.Chat.Id);

            var commandHandlers = scope.ServiceProvider.GetServices<ICommandHandler>();
            var commandPart = messageText.Split(' ')[0];

            var handler = commandHandlers
                .FirstOrDefault(h => h.Command.Equals(commandPart, StringComparison.OrdinalIgnoreCase));

            if (handler is not null)
            {
                // Message.Text ni o'zgartirmaymiz — handler ichida to'liq text kerak
                await handler.HandleAsync(botClient, message, ct);
                return;
            }

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❓ Noma'lum buyruq.",
                cancellationToken: ct);
            return;
        }

        // State bo'lsa
        if (!string.IsNullOrEmpty(session.CurrentState))
        {
            await HandleStateLogic(botClient, message, session, sessionService, scope, ct);
            return;
        }

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "❓ Tushunmadim. Quyidagilardan birini tanlang:",
            replyMarkup: BotKeyboards.MainMenu(),
            cancellationToken: ct);
    }

    private async Task HandleStateLogic(
        ITelegramBotClient bot,
        Message msg,
        UserSession session,
        SessionService sessionService,
        IServiceScope scope,
        CancellationToken ct)
    {
        var stateHandlers = scope.ServiceProvider.GetServices<IStateHandler>();
        var handler = stateHandlers.FirstOrDefault(h => h.State == session.CurrentState);

        if (handler is not null)
        {
            await handler.HandleAsync(bot, msg, session, ct);
        }
        else
        {
            sessionService.ClearSession(msg.Chat.Id);
            await bot.SendMessage(
                msg.Chat.Id,
                "❌ Xatolik yuz berdi. Qaytadan boshlang.",
                replyMarkup: BotKeyboards.MainMenu(),
                cancellationToken: ct);
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "❌ Bot xatolik");
        return Task.CompletedTask;
    }

    private string NormalizeMenuCommand(string text) => text switch
    {
        "➕ Yangi navbat yaratish" => "/createtask",
        "📋 Mening navbatlarim" => "/mytasks",
        "🔔 Takliflarim" => "/mytasks",
        "⚙️ Sozlamalar" => "/start",
        _ => text
    };

    private string RemoveBotUsername(string command)
    {
        var atIndex = command.IndexOf('@');
        return atIndex > 0 ? command[..atIndex] : command;
    }
}