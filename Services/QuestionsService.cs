using QuizApi.Models;

namespace QuizApi.Services
{
    public class QuestionsService : IQuestionsService
    {
        public QuestionDTO QuestionToDTO(Question question) => new QuestionDTO
        {
            Id = question.Id,
            Category = question.Category,
            Title = question.Title,
            Difficulty = question.Difficulty
        };

        public bool IsAnswerRight(string userAnswer, List<string> acceptedAnswers)
        {
            return acceptedAnswers.Any(answer => string.Equals(userAnswer, answer, StringComparison.OrdinalIgnoreCase));
        }
    }
}
