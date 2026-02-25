namespace bot.Models;

public class UserSession
{
    public long UserId { get; set; }
    public string? CurrentState { get; set; }
    public string? TaskName { get; set; }
    public string? TaskDescription { get; set; }
    public long? TelegramGroupId { get; set; }
    public bool? SendToGroup { get; set; }
    public int? NotifyIntervalDays { get; set; }
    public TimeSpan? NotifyTime { get; set; }
    public int? EditingTaskId { get; set; }
}