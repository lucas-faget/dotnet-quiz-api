using QuizApi.Models;
using Newtonsoft.Json;
using System.Text;
using System.Globalization;

namespace QuizApi.Services
{
    public class QuestionsService : IQuestionsService
    {
        private static readonly double _minRightAnswerSimilarity = 0.85;
        private static readonly double _minAlmostRightAnswerSimilarity = 0.7;

        public QuestionDTO QuestionToDTO(Question question) => new()
        {
            Id = question.Id,
            Category = question.Category,
            Title = question.Title,
            Difficulty = question.Difficulty
        };

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

        public AnswerResult GetAnswerResult(string userAnswer, List<string> acceptedAnswers)
        {
            double maxSimilarity = 0.0;

            string normalizedUserAnswer = NormalizeString(userAnswer);

            foreach (string acceptedAnswer in acceptedAnswers)
            {
                double similarity = CalculateSimilarity(normalizedUserAnswer, NormalizeString(acceptedAnswer));

                maxSimilarity = similarity > maxSimilarity ? similarity : maxSimilarity;
            }

            if (maxSimilarity > _minRightAnswerSimilarity)
                return AnswerResult.Right;
            else if (maxSimilarity > _minAlmostRightAnswerSimilarity)
                return AnswerResult.AlmostRight;
            else
                return AnswerResult.Wrong;
        }

        /**
         * Remove white spaces
         * Remove diacritics
         * Lowercase
         */
        public static string NormalizeString(string text)
        {
            string normalizedString = text.Normalize(NormalizationForm.FormD);
    
            var stringBuilder = new StringBuilder(normalizedString.Length);
    
            foreach (char c in normalizedString)
            {
                if (!char.IsWhiteSpace(c) && CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(char.ToLower(c));
                }
            }
    
            return stringBuilder.ToString();
        }

        public static double CalculateSimilarity(string x, string y)
        {
            double maxLength = Math.Max(x.Length, y.Length);

            if (maxLength > 0) {
                return (maxLength - GetEditDistance(x, y)) / maxLength;
            }

            return 1.0;
        }

        public static int GetEditDistance(string x, string y)
        {
            int m = x.Length;
            int n = y.Length;
    
            int[][] T = new int[m + 1][];
            for (int i = 0; i < m + 1; ++i) {
                T[i] = new int[n + 1];
            }
    
            for (int i = 1; i <= m; i++) {
                T[i][0] = i;
            }
            for (int j = 1; j <= n; j++) {
                T[0][j] = j;
            }
    
            int cost;
            for (int i = 1; i <= m; i++) {
                for (int j = 1; j <= n; j++) {
                    cost = x[i - 1] == y[j - 1] ? 0: 1;
                    T[i][j] = Math.Min(Math.Min(T[i - 1][j] + 1, T[i][j - 1] + 1), T[i - 1][j - 1] + cost);
                }
            }
    
            return T[m][n];
        }
    }
}
