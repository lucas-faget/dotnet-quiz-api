using System.ComponentModel.DataAnnotations;

namespace QuizApi.Models;

public class Room
{
    [Key]
    public string Code { get; set; } = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 4).Select(s => s[new Random().Next(s.Length)]).ToArray());

    public List<Player> Players { get; } = [];
    
    public Game? Game { get; set; }
}
