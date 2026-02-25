namespace bot.Entities;

public class TaskUser
{
    public long Id { get; set; }
    public int TaskId { get; set; }
    public Task Task { get; set; } = null!;
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public int QueuePosition { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsCurrent { get; set; } = false;
    public bool IsPendingConfirmation { get; set; } = false;
    public TaskUserRole Role { get; set; } = TaskUserRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UserQueueTime { get; set; }
    public DateTime? PendingConfirmationSince { get; set; }
    public int TotalServedCount { get; set; } = 0;
    public int RejectionCount { get; set; } = 0;
}
public enum TaskUserRole
{
    Member = 0,
    Admin = 1
}