namespace QuizApi.Models;

public class ScoreDTO
{
    public bool HasAnsweredRight { get; set; } = false;
    public int Points { get; set; } = 0;
    public int? Order { get; set; }
}
