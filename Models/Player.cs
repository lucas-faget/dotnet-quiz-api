using System.ComponentModel.DataAnnotations;

namespace QuizApi.Models;

public class Player
{
    [Key]
    public string ConnectionId { get; set; } = null!;

    public string? Name { get; set; }
    
    public long RoomId { get; set; }
}
