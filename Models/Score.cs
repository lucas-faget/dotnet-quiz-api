using System.ComponentModel.DataAnnotations;

namespace QuizApi.Models;

public class Score
{
    [Key]
    public string PlayerId { get; set; } = null!;

    [Key]
    public long GameId { get; set; }

    [Key]
    public long QuestionId { get; set; }

    public int Tries { get; set; } = 0;

    public bool HasAnsweredRight { get; set; } = false;

    public int Points { get; set; } = 0;
    
    public int? Order { get; set; }
}
