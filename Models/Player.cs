using System.ComponentModel.DataAnnotations;

namespace QuizApi.Models;

public class Player
{
    public long Id { get; set; }

    [Required]
    public string? Name { get; set; }
}
