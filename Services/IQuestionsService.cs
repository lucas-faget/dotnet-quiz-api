using QuizApi.Models;

namespace QuizApi.Services
{
    public interface IQuestionsService
    {
        public QuestionDTO QuestionToDTO(Question question);
        public bool IsAnswerRight(string userAnswer, List<string> acceptedAnswers);
    }
}
