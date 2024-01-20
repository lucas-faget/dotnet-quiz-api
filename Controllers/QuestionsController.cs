using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizApi.Models;

namespace QuizApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestionsController : ControllerBase
    {
        private readonly QuestionContext _context;

        public QuestionsController(QuestionContext context)
        {
            _context = context;
        }

        // GET: api/Questions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Question>>> GetQuestions()
        {
            return await _context.Questions.ToListAsync();
        }

        // GET: api/Questions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<QuestionDTO>> GetQuestion(long id)
        {
            var question = await _context.Questions.FindAsync(id);

            if (question == null)
            {
                return NotFound();
            }

            return QuestionToDTO(question);
        }

        // PUT: api/Questions/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutQuestion(long id, Question question)
        {
            if (id != question.Id)
            {
                return BadRequest();
            }

            _context.Entry(question).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!QuestionExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Questions
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Question>> PostQuestion(Question question)
        {
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            // return CreatedAtAction("GetQuestion", new { id = question.Id }, question);
            return CreatedAtAction(nameof(GetQuestion), new { id = question.Id }, question);
        }

        // DELETE: api/Questions/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestion(long id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null)
            {
                return NotFound();
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool QuestionExists(long id)
        {
            return _context.Questions.Any(e => e.Id == id);
        }

        private static QuestionDTO QuestionToDTO(Question question) => new QuestionDTO
        {
            Id = question.Id,
            Category = question.Category,
            Title = question.Title,
            Difficulty = question.Difficulty
        };

        // POST: api/Questions/5/Answer
        [HttpPost("{id}/Answer")]
        public async Task<ActionResult> CheckAnswer(long id, [FromBody] string userAnswer)
        {
            var question = await _context.Questions.FindAsync(id);

            if (question == null)
            {
                return NotFound();
            }

            if (question.AcceptedAnswers != null && IsAnswerRight(userAnswer, question.AcceptedAnswers))
            {
                return Ok(new { Result = AnswerResult.Right });
            }
            else
            {
                return Ok(new { Result = AnswerResult.Wrong });
            }
        }

        private bool IsAnswerRight(string userAnswer, string[] acceptedAnswers)
        {
            return acceptedAnswers.Any(answer => string.Equals(userAnswer, answer, StringComparison.OrdinalIgnoreCase));
        }
    }
}
