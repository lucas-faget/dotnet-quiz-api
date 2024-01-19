namespace QuizApi.Models;

public class Question
{
    public long Id { get; set; }
    public string? Category { get; set; }
    public string? Title { get; set; }
    public string? Answer { get; set; }
    public string? AcceptedAnswers { get; set; }
    public string? Difficulty { get; set; }
}
