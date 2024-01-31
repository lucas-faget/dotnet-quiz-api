namespace QuizApi.Models;

public class Game
{
    public long Id { get; set; }
    public int QuestionIndex { get; set; } = 0;
    public Room Room { get; set; } = null!;
    public List<Question> Questions { get; set; } = [];
}
