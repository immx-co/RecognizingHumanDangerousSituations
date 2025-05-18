namespace ClassLibrary.Database.Models;

public class User
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string HashPassword { get; set; }

    public string? Email { get; set; }

    public bool IsAdmin { get; set; } = false;

    public long? TgChatId { get; set; } = null;
}
