namespace bot.Entities;

public class Task
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public long CreatedUserId { get; set; }
    public long? TelegramGroupId { get; set; }
    public bool SendToGroup { get; set; } = false;
    public string InviteToken { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public bool InviteIsActive { get; set; } = true;
    public int MaxMembers { get; set; } = 50;
    public int NotifyIntervalDays { get; set; } = 1;
    public TimeSpan NotifyTime { get; set; } = new TimeSpan(9, 0, 0);
    public DateTime? LastNotifiedAt { get; set; }
    public int ConfirmationHours { get; set; } = 24;
    public ICollection<TaskUser> TaskUsers { get; set; } = new List<TaskUser>();
}