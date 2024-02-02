namespace QuizApi.Models;

public class Game
{
    public long Id { get; set; }
    public bool CanAnswer { get; set; } = false;
    public Room Room { get; set; } = null!;
    public List<Question> Questions { get; set; } = [];
}
