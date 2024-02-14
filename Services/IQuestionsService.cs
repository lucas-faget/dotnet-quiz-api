using QuizApi.Models;

namespace QuizApi.Services
{
    public interface IQuestionsService
    {
        QuestionDTO QuestionToDTO(Question question);
        List<Question> LoadQuestionsFromJson();
        public AnswerResult GetAnswerResult(string userAnswer, List<string> acceptedAnswers);
    }
}
