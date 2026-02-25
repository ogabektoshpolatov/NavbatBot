using bot.Data;
using bot.Handlers;
using bot.Handlers.Callbacks;
using bot.Handlers.StateHandlers;
using bot.Handlers.TaskCommands;
using bot.Sercvices;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

// Telegram Bot
builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var token = config["TelegramBot:Token"]
        ?? throw new NullReferenceException("TelegramBot:Token");
    return new TelegramBotClient(token);
});

// Background Services
builder.Services.AddHostedService<TelegramBotService>();
builder.Services.AddHostedService<NotificationSchedulerService>();
builder.Services.AddHostedService<PendingConfirmationChecker>();

// Core Services
builder.Services.AddSingleton<SessionService>();
builder.Services.AddScoped<TaskNotificationService>();
builder.Services.AddScoped<TaskUiRenderer>();
builder.Services.AddScoped<QueueManagementService>();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Command Handlers ──────────────────────────────
builder.Services.AddScoped<ICommandHandler, StartCommandHandler>();
builder.Services.AddScoped<ICommandHandler, UsersCommandHandler>();
builder.Services.AddScoped<ICommandHandler, CreateTaskCommandHandler>();
builder.Services.AddScoped<ICommandHandler, GetTasksCommandHandler>();
builder.Services.AddScoped<ICommandHandler, GetGroupIdCommandHandler>();

// ── State Handlers ────────────────────────────────
builder.Services.AddScoped<IStateHandler, AwaitingTaskNameHandler>();
builder.Services.AddScoped<IStateHandler, AwaitingTaskDescriptionHandler>();
builder.Services.AddScoped<IStateHandler, AwaitingGroupIdHandler>();
builder.Services.AddScoped<IStateHandler, AwaitingNotifyTimeHandler>();
builder.Services.AddScoped<IStateHandler, AwaitingNotifyIntervalHandler>();
builder.Services.AddScoped<IStateHandler, AwaitingAutoNotifyHandler>();
builder.Services.AddScoped<IStateHandler, EditingTaskNameHandler>();
builder.Services.AddScoped<IStateHandler, EditingTaskDescriptionHandler>();
builder.Services.AddScoped<IStateHandler, EditingGroupIdHandler>();

// ── Callback Handlers ─────────────────────────────

// Task menu
builder.Services.AddScoped<ICallbackHandler, TaskMenuCallbackHandler>();

// View / Queue
builder.Services.AddScoped<ICallbackHandler, ViewTaskCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, MemberTaskCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, SkipQueueCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, AssignUserToQueueHandler>();
builder.Services.AddScoped<ICallbackHandler, AssignUserToQueueConfirmHandler>();
builder.Services.AddScoped<ICallbackHandler, CompleteTaskCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, AcceptQueueCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, RejectQueueCallbackHandler>();

// Add / Remove user
builder.Services.AddScoped<ICallbackHandler, AddUserCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, AddUserToTaskConfirmCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, DeleteUserCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, DeleteUserConfirmCallbackHandler>();

// Join
builder.Services.AddScoped<ICallbackHandler, JoinConfirmCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, JoinCancelCallbackHandler>();

// Edit
builder.Services.AddScoped<ICallbackHandler, TaskEditMenuCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, TaskEditNameCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, TaskEditDescriptionCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, TaskEditGroupIdCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, TaskEditNotifyTimeCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, TaskEditNotifyTimeSetCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, TaskEditIntervalCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, TaskEditIntervalSetCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, TaskEditSendToGroupCallbackHandler>();

// Invite
builder.Services.AddScoped<ICallbackHandler, TaskInviteCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, TaskInviteRefreshCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, TaskInviteToggleCallbackHandler>();

// Reorder
builder.Services.AddScoped<ICallbackHandler, TaskReorderCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, TaskReorderUpCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, TaskReorderDownCallbackHandler>();

// Delete
builder.Services.AddScoped<ICallbackHandler, TaskDeleteCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, TaskDeleteConfirmCallbackHandler>();

// Notify (create flow)
builder.Services.AddScoped<ICallbackHandler, NotifyCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, CreateTimeCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, CreateIntervalCallbackHandler>();

var app = builder.Build();

// Migration
if (builder.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    Console.WriteLine("✅ Database migrated");
}
else if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapGet("/test-notification", async (TaskNotificationService notificationService) =>
{
    await notificationService.SendDailyNotifications();
    return Results.Ok(new { message = "Notification sent!", timestamp = DateTime.UtcNow });
});

app.Run();