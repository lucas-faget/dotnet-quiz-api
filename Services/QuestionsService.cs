using QuizApi.Models;
using Newtonsoft.Json;
using System.Text;
using System.Globalization;

namespace QuizApi.Services
{
    public class QuestionsService : IQuestionsService
    {
        public QuestionDTO QuestionToDTO(Question question) => new()
        {
            Id = question.Id,
            Category = question.Category,
            Title = question.Title,
            Difficulty = question.Difficulty
        };

        public bool IsAnswerRight(string userAnswer, List<string> acceptedAnswers)
        {
            return acceptedAnswers.Any(answer => string.Equals(RemoveDiacriticsAndWhiteSpaces(userAnswer), RemoveDiacriticsAndWhiteSpaces(answer), StringComparison.OrdinalIgnoreCase));
        }

        public static string RemoveDiacriticsAndWhiteSpaces(string text)
        {
            string normalizedString = text.Normalize(NormalizationForm.FormD);
    
            var stringBuilder = new StringBuilder(normalizedString.Length);
    
            foreach (char c in normalizedString)
            {
                if (!char.IsWhiteSpace(c) && CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }
    
            return stringBuilder.ToString();
        }

        public List<Question> LoadQuestionsFromJson()
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
}
