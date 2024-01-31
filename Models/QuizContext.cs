using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace QuizApi.Models;

public class QuizContext : DbContext
{
    public QuizContext(DbContextOptions<QuizContext> options)
        : base(options)
    {
    }

    public DbSet<Question> Questions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var questions = LoadQuestionsFromJson();

        modelBuilder.Entity<Question>().HasData(questions);
    }

    private static List<Question> LoadQuestionsFromJson()
    {
        string jsonFilePath = "Questions.json";

        if (!File.Exists(jsonFilePath))
        {
            return [];
        }

        string json = File.ReadAllText(jsonFilePath);

        var questions = JsonConvert.DeserializeObject<List<Question>>(json);

        return questions ?? [];
    }
}
