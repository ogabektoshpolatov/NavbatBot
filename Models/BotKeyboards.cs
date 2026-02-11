using bot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using User = bot.Entities.User;

namespace bot.Models;

public class BotKeyboards
{
    public static InlineKeyboardMarkup TaskMenu(int taskId) => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("➕ User qo'shish",     CB.AddUser(taskId))    },
        new[] { InlineKeyboardButton.WithCallbackData("➖ User o'chirish",    CB.RemoveUser(taskId)) },
        new[] { InlineKeyboardButton.WithCallbackData("👥 Userlarni ko'rish", CB.ViewUsers(taskId))  },
    });
    
    public static InlineKeyboardMarkup BackToTask(int taskId) => new(new[]
    {
        new[] { InlineKeyboardButton.WithCallbackData("🔙 Orqaga", CB.Task(taskId)) }
    });
    
    public static InlineKeyboardMarkup AddUserList(int taskId, List<User> users)
    {
        var buttons = users
            .Select(u =>
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"👤 {u.Username}",
                        $"task:{taskId}:selectUser:{u.UserId}")
                })
            .ToList();

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("⬅ Back", $"task:{taskId}")
        });

        return new InlineKeyboardMarkup(buttons);
    }
}