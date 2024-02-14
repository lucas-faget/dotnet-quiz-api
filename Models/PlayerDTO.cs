namespace QuizApi.Models;

public class PlayerDTO
{
    public string Name { get; set; } = null!;
    public int TotalPoints { get; set; } = 0;
    public ScoreDTO? Score { get; set; }
}
