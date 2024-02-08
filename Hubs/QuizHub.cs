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
        private static readonly Dictionary<long, Room> _rooms = [];

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
                RoomId = room.Id
            });

            _rooms.Add(room.Id, room);

            await Groups.AddToGroupAsync(Context.ConnectionId, room.Id.ToString());

            await SendPlayers(room.Id, room.Players);

            await SendMessage(room.Id, $"{playerName} created the room.");

            await StartGame(room.Id);
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
                    RoomId = roomId
                });

                await SendPlayers(roomId, room.Players);

                await SendMessage(roomId, $"{playerName} has joined.");
            }
            else
            {
                await CreateRoom(playerName);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);

            var player = FindPlayerByConnectionId(Context.ConnectionId);

            if (player != null)
            {
                if (_rooms.TryGetValue(player.RoomId, out Room? room))
                {
                    room.Players.RemoveAll(player => player.ConnectionId == Context.ConnectionId);

                    if (room.Players.Count == 0)
                    {
                        _rooms.Remove(player.RoomId);
                    }

                    await SendPlayers(player.RoomId, room.Players);

                    await SendMessage(player.RoomId, $"{player.Name} has left.");
                }
            }
        }

        public async Task SendPlayers(long roomId, List<Player> players)
        {
            await Clients.Group(roomId.ToString()).SendAsync("ReceivePlayers", players);
        }

        public async Task SendMessage(long roomId, string message)
        {
            await Clients.Group(roomId.ToString()).SendAsync("ReceiveMessage", message);
        }

        public async Task SendUserMessage(long roomId, string message)
        {
            if (_rooms.TryGetValue(roomId, out Room? room))
            {
                var player = room.Players.FirstOrDefault(player => player.ConnectionId == Context.ConnectionId);
                if (player != null)
                {
                    await Clients.GroupExcept(roomId.ToString(), Context.ConnectionId).SendAsync("ReceiveMessage", message, player.Name);
                }
            }

        }

        public async Task SendDelay(long roomId, int seconds)
        {
            await Clients.Group(roomId.ToString()).SendAsync("ReceiveDelay", seconds);
        }

        public async Task SendQuestion(long roomId, QuestionDTO question, int seconds)
        {
            await Clients.Group(roomId.ToString()).SendAsync("ReceiveQuestion", question, seconds);
        }

        public async Task CheckAnswer(long roomId, long questionId, string userAnswer)
        {
            if (_rooms.TryGetValue(roomId, out Room? room))
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

        public async Task SendAnswer(long roomId, string answer)
        {
            if (_rooms.ContainsKey(roomId))
            {
                await Clients.Group(roomId.ToString()).SendAsync("ReceiveAnswer", answer);
            }
        }

        public async Task StartGame(long roomId)
        {
            if (_rooms.TryGetValue(roomId, out Room? room))
            {
                var randomQuestions = await _context.Questions
                    .OrderBy(q => Guid.NewGuid())
                    .Take(50)
                    .ToListAsync();

                var game = new Game {
                    Room = room,
                    Questions = randomQuestions
                };

                room.Game = game;

                await SendMessage(roomId, "Game has started");

                foreach (var question in game.Questions)
                {
                    await SendDelay(roomId, 10);

                    await Task.Delay(TimeSpan.FromSeconds(10));
                        
                    var questionDTO = _questionsService.QuestionToDTO(question);

                    game.CanAnswer = true;

                    await SendQuestion(roomId, questionDTO, 20);

                    await Task.Delay(TimeSpan.FromSeconds(20));

                    game.CanAnswer = false;

                    await SendAnswer(roomId, question.Answer);
                }

                await SendMessage(roomId, "Game is over");
            }
        }

        public static Player? FindPlayerByConnectionId(string id)
        {
            return _rooms.SelectMany(room => room.Value.Players).FirstOrDefault(player => player.ConnectionId == id);
        }
    }
}
