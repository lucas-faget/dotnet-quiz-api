using System.ComponentModel.DataAnnotations;

namespace QuizApi.Models;

public class Question
{
    public long Id { get; set; }

    public string? Category { get; set; }

    [Required]
    public string? Title { get; set; }

    [Required]
    public string? Answer { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "The AcceptedAnswers field cannot be empty.")]
    public List<string> AcceptedAnswers { get; set; } = [];

    public string? Difficulty { get; set; }
}
