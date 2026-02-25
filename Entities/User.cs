using System.ComponentModel.DataAnnotations;

namespace bot.Entities;

public class User
{
    [Key]
    public long UserId { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsBanned { get; set; } = false;
    public string? LanguageCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<TaskUser> TaskUsers { get; set; } = new List<TaskUser>();
}