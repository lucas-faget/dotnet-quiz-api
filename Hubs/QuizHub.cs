using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuizApi.Models;
using QuizApi.Services;
using System.Timers;
using Timer = System.Timers.Timer;

namespace QuizApi.Hubs
{
    public class QuizHub : Hub
    {
        private readonly QuizContext _context;
        private readonly IQuestionsService _questionsService;
        private static readonly Dictionary<long, Room> _rooms = [];
        private static readonly Dictionary<long, Timer> _timers = [];

        public QuizHub(QuizContext context, IQuestionsService questionsService)
        {
            _context = context;
            _questionsService = questionsService;
        }

        public async Task CreateRoom(string playerName = "")
        {
            playerName = string.IsNullOrEmpty(playerName) ? "Player 1" : playerName;

            var room = new Room();

            room.Players.Add(new Player {
                ConnectionId = Context.ConnectionId,
                Name = playerName,
                Room = room
            });

            _rooms.Add(room.Id, room);

            await Groups.AddToGroupAsync(Context.ConnectionId, room.Id.ToString());

            await SendMessage(room.Id, $"{playerName} created the room.");

            await NewGame(room.Id);
        }

        public async Task JoinRoom(long roomId, string playerName = "")
        {
            if (_rooms.TryGetValue(roomId, out Room? room))
            {
                playerName = string.IsNullOrEmpty(playerName) ? $"Player {room.Players.Count + 1}" : playerName;

                await Groups.AddToGroupAsync(Context.ConnectionId, room.Id.ToString());

                room.Players.Add(new Player {
                    ConnectionId = Context.ConnectionId,
                    Name = playerName,
                    Room = room
                });

                await SendMessage(roomId, $"{playerName} has joined.");
            }
            else
            {
                await CreateRoom(playerName);
            }
        }

        public async Task NewGame(long roomId)
        {
            if (_rooms.TryGetValue(roomId, out Room? room))
            {
                var randomQuestions = await _context.Questions
                    .OrderBy(q => Guid.NewGuid())
                    .Take(3)
                    .ToListAsync();

                var game = new Game {
                    Room = room,
                    Questions = randomQuestions
                };

                room.Game = game;

                var questionDTO = GetCurrentQuestion(game);

                if (questionDTO != null)
                {
                    await SendQuestion(roomId, questionDTO);

                    await SendMessage(roomId, $"Question have been sent.");
                }

                await SendMessage(roomId, $"Game has started.");
            }
        }

        public QuestionDTO? GetCurrentQuestion(Game game)
        {
            if (game.QuestionIndex >= 0 && game.QuestionIndex < game.Questions.Count)
            {
                var question = game.Questions[game.QuestionIndex];

                var questionDTO = _questionsService.QuestionToDTO(question);

                return questionDTO;
            }
            
            return null;
        }

        public async Task SendMessage(long roomId, string message)
        {
            await Clients.Group(roomId.ToString()).SendAsync("ReceiveMessage", message);
        }

        public async Task SendQuestion(long roomId, QuestionDTO question)
        {
            await Clients.Group(roomId.ToString()).SendAsync("ReceiveQuestion", question);
        }

        public async Task CheckAnswer(long roomId, long questionId, string userAnswer)
        {
            if (_rooms.ContainsKey(roomId))
            {
                var question = await _context.Questions.FindAsync(questionId);

                if (question != null && question.AcceptedAnswers != null)
                {
                    var result = _questionsService.IsAnswerRight(userAnswer, question.AcceptedAnswers) ? AnswerResult.Right : AnswerResult.Wrong;

                    await Clients.Group(roomId.ToString()).SendAsync("ReceiveAnswerResult", result);
                }
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);

            var player = SearchPlayerById(Context.ConnectionId);

            if (player != null && player.Room != null)
            {
                player.Room.Players.RemoveAll(player => player.ConnectionId == Context.ConnectionId);

                if (player.Room.Players.Count == 0)
                {
                    _rooms.Remove(player.Room.Id);
                }

                await SendMessage(player.Room.Id, $"{player.Name} has left.");
            }
        }

        public Player? SearchPlayerById(string id)
        {
            foreach (var room in _rooms)
            {
                foreach (var player in room.Value.Players)
                {
                    if (player.ConnectionId == id)
                    {
                        return player;
                    }
                }
            }

            return null;
        }
    }
}
