using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuizApi.Models;
using QuizApi.Services;

namespace QuizApi.Hubs
{
    public class QuizHub : Hub
    {
        private readonly QuizContext _context;
        private readonly IQuestionsService _questionsService;
        private static readonly Dictionary<string, Room> _rooms = [];

        public QuizHub(QuizContext context, IQuestionsService questionsService)
        {
            _context = context;
            _questionsService = questionsService;
        }

        public Task<bool> RoomExists(string code)
        {
            return Task.FromResult(_rooms.ContainsKey(code));
        }

        public async Task<string> CreateRoom(string playerName = "")
        {
            var room = new Room();
            Console.WriteLine($"room {room.Code} created");

            _rooms.Add(room.Code, room);

            await Groups.AddToGroupAsync(Context.ConnectionId, room.Code);

            playerName = string.IsNullOrEmpty(playerName) ? "Player 1" : playerName;

            room.Players.Add(new Player {
                ConnectionId = Context.ConnectionId,
                Name = playerName,
                RoomCode = room.Code
            });

            await SendPlayers(room.Code, room.Players);

            await SendMessage(room.Code, $"{playerName} created the room.");

            await StartGame(room.Code);

            return room.Code;
        }

        public async Task JoinRoom(string code, string playerName = "")
        {
            if (_rooms.TryGetValue(code, out Room? room))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, room.Code);

                playerName = string.IsNullOrEmpty(playerName) ? $"Player {room.Players.Count + 1}" : playerName;

                room.Players.Add(new Player {
                    ConnectionId = Context.ConnectionId,
                    Name = playerName,
                    RoomCode = code
                });

                await SendPlayers(code, room.Players);

                await SendMessage(code, $"{playerName} has joined.");
            }
        }

        public async Task StartGame(string code)
        {
            if (_rooms.TryGetValue(code, out Room? room))
            {
                var randomQuestions = await _context.Questions
                    .OrderBy(q => Guid.NewGuid())
                    .Take(20)
                    .ToListAsync();

                var game = new Game {
                    Room = room,
                    Questions = randomQuestions
                };

                room.Game = game;

                await SendMessage(code, "Game has started");

                foreach (var question in game.Questions)
                {
                    await SendDelay(code, 5);

                    await Task.Delay(TimeSpan.FromSeconds(5));
                        
                    var questionDTO = _questionsService.QuestionToDTO(question);

                    game.CanAnswer = true;

                    await SendQuestion(code, questionDTO, 20, ++game.QuestionNumber, game.Questions.Count);

                    await Task.Delay(TimeSpan.FromSeconds(20));

                    game.CanAnswer = false;

                    await SendAnswer(code, question.Answer);
                }

                await SendMessage(code, "Game is over");
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);

            var player = FindPlayerByConnectionId(Context.ConnectionId);

            if (player != null)
            {
                if (_rooms.TryGetValue(player.RoomCode, out Room? room))
                {
                    room.Players.RemoveAll(player => player.ConnectionId == Context.ConnectionId);

                    if (room.Players.Count == 0)
                    {
                        _rooms.Remove(player.RoomCode);
                        Console.WriteLine($"room {player.RoomCode} removed");
                    }

                    await SendPlayers(player.RoomCode, room.Players);

                    await SendMessage(player.RoomCode, $"{player.Name} has left.");
                }
            }
        }

        public async Task SendPlayers(string code, List<Player> players)
        {
            await Clients.Group(code).SendAsync("ReceivePlayers", players);
        }

        public async Task SendMessage(string code, string message)
        {
            await Clients.Group(code).SendAsync("ReceiveMessage", message);
        }

        public async Task SendUserMessage(string code, string message)
        {
            if (_rooms.TryGetValue(code, out Room? room))
            {
                var player = room.Players.FirstOrDefault(player => player.ConnectionId == Context.ConnectionId);
                if (player != null)
                {
                    await Clients.GroupExcept(code, Context.ConnectionId).SendAsync("ReceiveMessage", message, player.Name);
                }
            }

        }

        public async Task SendDelay(string code, int seconds)
        {
            await Clients.Group(code).SendAsync("ReceiveDelay", seconds);
        }

        public async Task SendQuestion(string code, QuestionDTO question, int seconds, int questionNumber, int maxQuestionNumber)
        {
            await Clients.Group(code).SendAsync("ReceiveQuestion", question, seconds, questionNumber, maxQuestionNumber);
        }

        public async Task CheckAnswer(string code, long questionId, string userAnswer)
        {
            if (_rooms.TryGetValue(code, out Room? room))
            {
                if (room.Game?.CanAnswer == true)
                {
                    var question = await _context.Questions.FindAsync(questionId);

                    if (question != null && question.AcceptedAnswers != null)
                    {
                        var result = _questionsService.IsAnswerRight(userAnswer, question.AcceptedAnswers) ? AnswerResult.Right : AnswerResult.Wrong;

                        await Clients.Caller.SendAsync("ReceiveAnswerResult", result);
                    }
                }
            }
        }

        public async Task SendAnswer(string code, string answer)
        {
            if (_rooms.ContainsKey(code))
            {
                await Clients.Group(code).SendAsync("ReceiveAnswer", answer);
            }
        }

        public static Player? FindPlayerByConnectionId(string id)
        {
            return _rooms.SelectMany(room => room.Value.Players).FirstOrDefault(player => player.ConnectionId == id);
        }
    }
}
