using QuizApi.Models;

namespace QuizApi.Services
{
    public interface IQuestionsService
    {
        QuestionDTO QuestionToDTO(Question question);
        bool IsAnswerRight(string userAnswer, List<string> acceptedAnswers);
        List<Question> LoadQuestionsFromJson();
    }
}
