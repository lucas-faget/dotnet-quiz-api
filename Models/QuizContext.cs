using Microsoft.EntityFrameworkCore;

namespace QuizApi.Models;

public class QuizContext : DbContext
{
    public QuizContext(DbContextOptions<QuizContext> options)
        : base(options)
    {
    }

    public DbSet<Question> Questions { get; set; } = null!;
    public DbSet<Room> Rooms { get; set; } = null!;
    public DbSet<RoomQuestion> RoomQuestions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>()
            .HasMany(e => e.Questions)
            .WithMany()
            .UsingEntity<RoomQuestion>();
    }
}
