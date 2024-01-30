namespace QuizApi.Models;

public class QuestionDTO
{
    public long Id { get; set; }
    public string? Category { get; set; }
    public string Title { get; set; } = null!;
    public string? Difficulty { get; set; }
}
