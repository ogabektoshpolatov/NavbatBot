using bot.Handlers;
using bot.Handlers.StateHandlers;
using bot.Models;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot.Sercvices;

public class TelegramBotService : BackgroundService
{
    private readonly TelegramBotClient _botClient;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public TelegramBotService(
        IConfiguration configuration, 
        ILogger<TelegramBotService> logger, 
        IServiceProvider serviceProvider,
        SessionService sessionService)
    {
        var token = configuration["TelegramBot:Token"];
        Console.WriteLine("token");
        _botClient = new TelegramBotClient(token ?? throw new NullReferenceException(nameof(token)));
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _botClient.DeleteWebhook(dropPendingUpdates:true, stoppingToken);
        _botClient.StartReceiving(
            updateHandler : HandleUpdateAsync,
            errorHandler : HandleErrorAsync,
            receiverOptions : new ReceiverOptions {},
            cancellationToken: stoppingToken);
        
        var me = await _botClient.GetMe(stoppingToken);
        _logger.LogInformation($"Bot @{me.Username} started!");
    }
    
    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message) return;
        if (message.Text is not { } messageText) return;

        messageText = NormalizeMenuCommand(message.Text);
        _logger.LogInformation($"Received '{messageText}' from {message.From?.Username}");
        
        using var scope = _serviceProvider.CreateScope();
        var sessionService = scope.ServiceProvider.GetRequiredService<SessionService>();
        var session = sessionService.GetOrCreateSession(message.Chat.Id);

        if (messageText.StartsWith('/'))
        {
            sessionService.ClearSession(message.Chat.Id);
            var commandHandlers = scope.ServiceProvider.GetServices<ICommandHandler>();
            var handler = commandHandlers.FirstOrDefault(h => h.Command.Equals(messageText, StringComparison.OrdinalIgnoreCase));

            if (handler != null)
            {
                await handler.HandleAsync(botClient, message, cancellationToken);
                return;
            }
        }
        
        if (!string.IsNullOrEmpty(session.CurrentState))
        {
            await HandleStateLogic(botClient, message, session, sessionService, cancellationToken);
            return;
        }
        
        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "❌ Tushunmadim. /help ni yozing.",
            cancellationToken: cancellationToken
        );
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Error occurred");
        return Task.CompletedTask;
    }
    
    private async Task HandleStateLogic(ITelegramBotClient bot, Message msg, UserSession session, SessionService service, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var stateHandlers = scope.ServiceProvider.GetServices<IStateHandler>();
        Console.WriteLine(session.CurrentState);
        Console.WriteLine(session.TaskName);
        var handler = stateHandlers.FirstOrDefault(h => h.State == session.CurrentState);

        if (handler != null)
        {
            await handler.HandleAsync(bot, msg, session, ct);
        }
        else
        {
            service.ClearSession(msg.Chat.Id);
            await bot.SendMessage(msg.Chat.Id, "Xatolik yuz berdi, jarayon bekor qilindi.", cancellationToken: ct);
        }
    }
    private string NormalizeMenuCommand(string text)
    {
        return text switch
        {
            "➕ Create task" => "/createtask",
            "📋 My Tasks" => "/mytasks",
            _ => text
        };
    }
}