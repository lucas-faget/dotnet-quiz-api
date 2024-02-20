namespace QuizApi.Models;

public class Player
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int? PreviousRank { get; set; }
    public int Rank { get; set; } = 1;
    public int TotalPoints { get; set; } = 0;
    public string RoomCode { get; set; } = null!;
}
