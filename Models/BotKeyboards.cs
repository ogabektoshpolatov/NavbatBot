using bot.Entities;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot.Models;

public class BotKeyboards
{
    // === ASOSIY MENYU ===
    public static ReplyKeyboardMarkup MainMenu() => new(new[]
    {
        new KeyboardButton[] { "➕ Task yaratish", "📋 Mening tasklarim" },
        // new KeyboardButton[] { "🔔 Takliflarim", "⚙️ Sozlamalar" }
    })
    {
        ResizeKeyboard = true,
        OneTimeKeyboard = false
    };

    // === TASK MENYUSI (Owner/Admin) ===
    public static InlineKeyboardMarkup TaskMenu(int taskId) => new(new[]
    {
        new[] {
            InlineKeyboardButton.WithCallbackData("👥 Navbatni ko'rish", CB.TaskViewUsers(taskId)),
            InlineKeyboardButton.WithCallbackData("✏️ Tahrirlash", CB.TaskEdit(taskId))
        },
        new[] {
            InlineKeyboardButton.WithCallbackData("🔗 Invite link", CB.TaskInvite(taskId)),
            InlineKeyboardButton.WithCallbackData("🗑️ O'chirish", CB.TaskDelete(taskId))
        }
    });

    // === TASK TAHRIRLASH MENYUSI ===
    public static InlineKeyboardMarkup TaskEditMenu(int taskId, bool sendToGroup) => new(new[]
    {
        new[] {
            InlineKeyboardButton.WithCallbackData("📝 Nomi", CB.TaskEditName(taskId)),
            InlineKeyboardButton.WithCallbackData("📋 Tavsif", CB.TaskEditDescription(taskId))
        },
        new[] {
            InlineKeyboardButton.WithCallbackData("💬 Guruh ID", CB.TaskEditGroupId(taskId)),
            InlineKeyboardButton.WithCallbackData("🕐 Bildirishnoma vaqti", CB.TaskEditNotifyTime(taskId))
        },
        new[] {
            InlineKeyboardButton.WithCallbackData("📅 Interval", CB.TaskEditInterval(taskId)),
            InlineKeyboardButton.WithCallbackData(
                sendToGroup ? "🔔 Guruhga: ✅" : "🔔 Guruhga: ❌",
                CB.TaskEditSendToGroup(taskId))
        },
        new[] {
            InlineKeyboardButton.WithCallbackData("🔄 Navbat tartibi", CB.TaskReorder(taskId))
        },
        new[] {
            InlineKeyboardButton.WithCallbackData("⬅️ Orqaga", CB.Task(taskId))
        }
    });

    // === INTERVAL TANLASH ===
    public static InlineKeyboardMarkup IntervalSelector(int taskId) => new(new[]
    {
        new[] {
            InlineKeyboardButton.WithCallbackData("📅 Har kun", CB.TaskEditIntervalSet(taskId, 1)),
            InlineKeyboardButton.WithCallbackData("📅 Har 3 kun", CB.TaskEditIntervalSet(taskId, 3)),
            InlineKeyboardButton.WithCallbackData("📅 Har hafta", CB.TaskEditIntervalSet(taskId, 7))
        },
        new[] { InlineKeyboardButton.WithCallbackData("⬅️ Orqaga", CB.TaskEdit(taskId)) }
    });

    // === NOTIFY TIME TANLASH ===
    public static InlineKeyboardMarkup NotifyTimeSelector(int taskId) => new(new[]
    {
        new[] {
            InlineKeyboardButton.WithCallbackData("07:00", CB.TaskEditNotifyTimeSet(taskId, "07:00")),
            InlineKeyboardButton.WithCallbackData("08:00", CB.TaskEditNotifyTimeSet(taskId, "08:00")),
            InlineKeyboardButton.WithCallbackData("09:00", CB.TaskEditNotifyTimeSet(taskId, "09:00"))
        },
        new[] {
            InlineKeyboardButton.WithCallbackData("10:00", CB.TaskEditNotifyTimeSet(taskId, "10:00")),
            InlineKeyboardButton.WithCallbackData("12:00", CB.TaskEditNotifyTimeSet(taskId, "12:00")),
            InlineKeyboardButton.WithCallbackData("15:00", CB.TaskEditNotifyTimeSet(taskId, "15:00"))
        },
        new[] {
            InlineKeyboardButton.WithCallbackData("17:00", CB.TaskEditNotifyTimeSet(taskId, "17:00")),
            InlineKeyboardButton.WithCallbackData("18:00", CB.TaskEditNotifyTimeSet(taskId, "18:00")),
            InlineKeyboardButton.WithCallbackData("20:00", CB.TaskEditNotifyTimeSet(taskId, "20:00"))
        },
        new[] { InlineKeyboardButton.WithCallbackData("⬅️ Orqaga", CB.TaskEdit(taskId)) }
    });

    // === INVITE LINK MENYUSI ===
    public static InlineKeyboardMarkup InviteMenu(int taskId, bool isActive) => new(new[]
    {
        new[] {
            InlineKeyboardButton.WithCallbackData("🔄 Linkni yangilash", CB.TaskInviteRefresh(taskId))
        },
        new[] {
            InlineKeyboardButton.WithCallbackData(
                isActive ? "🔒 Linkni yopish" : "🔓 Linkni ochish",
                CB.TaskInviteToggle(taskId))
        },
        new[] { InlineKeyboardButton.WithCallbackData("⬅️ Orqaga", CB.Task(taskId)) }
    });

    // === JOIN TUGMALARI ===
    public static InlineKeyboardMarkup JoinButtons(int taskId) => new(new[]
    {
        new[] {
            InlineKeyboardButton.WithCallbackData("✅ Qo'shilish", CB.JoinConfirm(taskId)),
            InlineKeyboardButton.WithCallbackData("❌ Bekor", CB.JoinCancel(taskId))
        }
    });

    // === NAVBAT KO'RISH MENYUSI ===
    public static InlineKeyboardMarkup QueueViewMenu(int taskId) => new(new[]
    {
        new[] {
            InlineKeyboardButton.WithCallbackData("⏭️ Navbatni o'tkazish", CB.TaskSkipQueue(taskId)),
            InlineKeyboardButton.WithCallbackData("👤 Navbatchi tayinlash", CB.TaskAssignQueue(taskId))
        },
        new[] {
            // InlineKeyboardButton.WithCallbackData("➕ User qo'shish", CB.TaskAddUser(taskId)),
            InlineKeyboardButton.WithCallbackData("➖ Navbatdan chiqarish", CB.TaskRemoveUser(taskId))
        },
        new[] { InlineKeyboardButton.WithCallbackData("⬅️ Orqaga", CB.Task(taskId)) }
    });

    // === USER RO'YXATI (qo'shish/o'chirish uchun) ===
    public static InlineKeyboardMarkup ViewUserList(int taskId, string order, List<User> users)
    {
        var buttons = users.Select(u => new[]
        {
            InlineKeyboardButton.WithCallbackData(
                $"👤 {u.FirstName ?? u.Username ?? "User"}",
                $"task:{taskId}:{order}:{u.UserId}:confirm")
        }).ToList();

        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Orqaga", CB.TaskViewUsers(taskId)) });
        return new InlineKeyboardMarkup(buttons);
    }

    // === NAVBAT TARTIBI (reorder) ===
    public static InlineKeyboardMarkup ReorderMenu(int taskId, List<TaskUser> taskUsers)
    {
        var buttons = taskUsers.OrderBy(tu => tu.QueuePosition).Select(tu => new[]
        {
            InlineKeyboardButton.WithCallbackData(
                $"{tu.QueuePosition}. {tu.User?.FirstName ?? "User"}", 
                $"noop"),
            InlineKeyboardButton.WithCallbackData("⬆️", CB.TaskReorderUp(taskId, tu.UserId)),
            InlineKeyboardButton.WithCallbackData("⬇️", CB.TaskReorderDown(taskId, tu.UserId))
        }).ToList();

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("✅ Saqlash", CB.TaskReorderSave(taskId)),
            InlineKeyboardButton.WithCallbackData("⬅️ Orqaga", CB.TaskViewUsers(taskId))
        });

        return new InlineKeyboardMarkup(buttons);
    }

    // === O'CHIRISH TASDIQLASH ===
    public static InlineKeyboardMarkup DeleteConfirm(int taskId) => new(new[]
    {
        new[] {
            InlineKeyboardButton.WithCallbackData("✅ Ha, o'chirish", CB.TaskDeleteConfirm(taskId)),
            InlineKeyboardButton.WithCallbackData("❌ Bekor", CB.Task(taskId))
        }
    });

    // === ORQAGA TUGMASI ===
    public static InlineKeyboardMarkup BackToTask(int taskId) => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("⬅️ Orqaga", CB.Task(taskId)) }
    });

    // === NAVBAT TUGAYTISH TUGMASI ===
    public static InlineKeyboardMarkup TaskCompletionButton(int taskId, long userId) => new(new[]
    {
        new[] {
            InlineKeyboardButton.WithCallbackData(
                "✅ Navbatchilikni tugatdim",
                $"complete_task:{taskId}:{userId}")
        }
    });

    // === QUEUE CONFIRMATION ===
    public static InlineKeyboardMarkup QueueConfirmationButtons(int taskId, long userId) => new(new[]
    {
        new[] {
            InlineKeyboardButton.WithCallbackData("✅ Qabul qilaman", $"accept_queue:{taskId}:{userId}"),
            InlineKeyboardButton.WithCallbackData("❌ Rad etaman", $"reject_queue:{taskId}:{userId}")
        }
    });

    // === NOTIFY TUGMASI (yaratishda) ===
    public static InlineKeyboardMarkup NotifyYesNo() => new(new[]
    {
        new[] {
            InlineKeyboardButton.WithCallbackData("✅ Ha", "notify_yes"),
            InlineKeyboardButton.WithCallbackData("❌ Yo'q", "notify_no")
        }
    });

    // === INTERVAL TANLASH (yaratishda) ===
    public static InlineKeyboardMarkup IntervalSelectorCreate() => new(new[]
    {
        new[] {
            InlineKeyboardButton.WithCallbackData("📅 Har kun", "create_interval:1"),
            InlineKeyboardButton.WithCallbackData("📅 Har 3 kun", "create_interval:3"),
            InlineKeyboardButton.WithCallbackData("📅 Har hafta", "create_interval:7")
        }
    });

    // === NOTIFY TIME TANLASH (yaratishda) ===
    public static InlineKeyboardMarkup NotifyTimeSelectorCreate() => new(new[]
    {
        new[] {
            InlineKeyboardButton.WithCallbackData("07:00", "create_time:07:00"),
            InlineKeyboardButton.WithCallbackData("08:00", "create_time:08:00"),
            InlineKeyboardButton.WithCallbackData("09:00", "create_time:09:00")
        },
        new[] {
            InlineKeyboardButton.WithCallbackData("10:00", "create_time:10:00"),
            InlineKeyboardButton.WithCallbackData("12:00", "create_time:12:00"),
            InlineKeyboardButton.WithCallbackData("15:00", "create_time:15:00")
        },
        new[] {
            InlineKeyboardButton.WithCallbackData("17:00", "create_time:17:00"),
            InlineKeyboardButton.WithCallbackData("18:00", "create_time:18:00"),
            InlineKeyboardButton.WithCallbackData("20:00", "create_time:20:00")
        }
    });
}