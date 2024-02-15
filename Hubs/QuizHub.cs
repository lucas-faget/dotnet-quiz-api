using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;
using QuizApi.Models;
using QuizApi.Services;

namespace QuizApi.Hubs
{
    public class QuizHub : Hub
    {
        private static readonly int _questionCount = 20;
        private static readonly int _timeToAnswerInSeconds = 20;
        private static readonly int _timeBetweenTwoQuestionsInSeconds = 5;
        private static readonly int _maxAnswerTries = 3;
        private static readonly int _minPointsByQuestion = 5;
        private static readonly int _maxPointsByQuestion = 8;
        private readonly QuizContext _context;
        private readonly IQuestionsService _questionsService;
        private static readonly Dictionary<string, Room> _rooms = [];
        private static int _gameCount = 0;

        public QuizHub(QuizContext context, IQuestionsService questionsService)
        {
            _context = context;
            _questionsService = questionsService;
        }

        public Task<bool> RoomExists(string code)
        {
            return Task.FromResult(_rooms.ContainsKey(code));
        }

        public async Task<string> CreateRoom(string playerName)
        {
            Room room = new();

            _rooms.Add(room.Code, room);

            await Groups.AddToGroupAsync(Context.ConnectionId, room.Code);

            room.Players.Add(new Player {
                Id = Context.ConnectionId,
                Name = playerName,
                RoomCode = room.Code
            });

            await SendPlayerScores(room.Code);

            await SendMessage(room.Code, $"{playerName} has created the room.");

            await SendMessage(room.Code, $"Code : {room.Code}");

            return room.Code;
        }

        public async Task JoinRoom(string code, string playerName)
        {
            if (_rooms.TryGetValue(code, out Room? room))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, room.Code);

                room.Players.Add(new Player {
                    Id = Context.ConnectionId,
                    Name = playerName,
                    RoomCode = code
                });

                if (room.Game != null)
                {
                    var questionId = room.Game.QuestionNumber > 0 ? room.Game.Questions[room.Game.QuestionNumber - 1]?.Id : null;

                    await SendPlayerScores(code, questionId);
                }

                await SendMessage(code, $"{playerName} has joined.");
            }
        }

        public async Task StartGame(string code)
        {
            if (_rooms.TryGetValue(code, out Room? room))
            {
                var randomQuestions = await _context.Questions
                    .OrderBy(q => Guid.NewGuid())
                    .Take(_questionCount)
                    .ToListAsync();

                var game = new Game {
                    Id = ++_gameCount,
                    Room = room,
                    Questions = randomQuestions
                };

                room.Game = game;

                await SendMessage(code, "Game has started.");

                foreach (var question in game.Questions)
                {
                    await SendDelay(code, _timeBetweenTwoQuestionsInSeconds);

                    await Task.Delay(TimeSpan.FromSeconds(_timeBetweenTwoQuestionsInSeconds));
                        
                    var questionDTO = _questionsService.QuestionToDTO(question);

                    foreach (var player in room.Players)
                    {
                        game.Scores.Add(new Score {
                            PlayerId = player.Id,
                            GameId = game.Id,
                            QuestionId = question.Id
                        });
                    }

                    await SendPlayerScores(code, question.Id);

                    game.RightAnswerNumber = 0;
                    game.CanAnswer = true;

                    await SendQuestion(code, questionDTO, _timeToAnswerInSeconds, ++game.QuestionNumber, game.Questions.Count);

                    await Task.Delay(TimeSpan.FromSeconds(_timeToAnswerInSeconds));

                    game.CanAnswer = false;

                    await SendAnswer(code, question.Answer);
                }

                await SendPlayerScores(code);

                await SendMessage(code, "Game is over.");
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);

            var player = FindPlayerById(Context.ConnectionId);

            if (player != null)
            {
                if (_rooms.TryGetValue(player.RoomCode, out Room? room))
                {
                    room.Players.RemoveAll(player => player.Id == Context.ConnectionId);

                    if (room.Players.Count == 0)
                    {
                        _rooms.Remove(player.RoomCode);
                    }

                    if (room.Game != null)
                    {
                        var questionId = room.Game.QuestionNumber > 0 ? room.Game.Questions[room.Game.QuestionNumber - 1]?.Id : null;

                        await SendPlayerScores(player.RoomCode, questionId);
                    }

                    await SendMessage(player.RoomCode, $"{player.Name} has left.");
                }
            }
        }

        public async Task SendPlayers(string code, List<PlayerDTO> players)
        {
            await Clients.Group(code).SendAsync("ReceivePlayers", players);
        }

        public async Task SendPlayerScores(string code, long? questionId = null)
        {
            List<PlayerDTO> playerScores = [];

            if (_rooms.TryGetValue(code, out Room? room))
            {
                foreach (var player in room.Players)
                {
                    PlayerDTO playerScore = new() {
                        Name = player.Name
                    };

                    if (questionId != null)
                    {
                        var score = room.Game?.Scores.FirstOrDefault(score => score.PlayerId == player.Id && score.GameId == room.Game.Id && score.QuestionId == questionId);

                        if (score != null)
                        {
                            playerScore.Score = ScoreToDTO(score) ?? null;
                        }
                    }

                    playerScore.TotalPoints = player.TotalPoints;

                    playerScores.Add(playerScore);
                }
            }

            var sortedPlayerScores = playerScores.OrderByDescending(player => player.TotalPoints).ToList();

            await SendPlayers(code, sortedPlayerScores);
        }

        public async Task SendMessage(string code, string message)
        {
            await Clients.Group(code).SendAsync("ReceiveMessage", message);
        }

        public async Task SendUserMessage(string code, string message)
        {
            if (_rooms.TryGetValue(code, out Room? room))
            {
                var player = room.Players.FirstOrDefault(player => player.Id == Context.ConnectionId);
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
                    var player = FindPlayerById(Context.ConnectionId);

                    if (player != null)
                    {
                        var score = room.Game.Scores.FirstOrDefault(score => score.PlayerId == player.Id && score.QuestionId == questionId);

                        if (score != null && score.Tries < _maxAnswerTries)
                        {
                            var question = await _context.Questions.FindAsync(questionId);

                            if (question != null && question.AcceptedAnswers != null)
                            {
                                AnswerResult result = _questionsService.GetAnswerResult(userAnswer, question.AcceptedAnswers);

                                score.Tries++;

                                if (result == AnswerResult.Right)
                                {
                                    score.HasAnsweredRight = true;
                                    score.Points = Math.Max(_minPointsByQuestion, _maxPointsByQuestion - room.Game.RightAnswerNumber);
                                    score.Order = ++room.Game.RightAnswerNumber;

                                    player.TotalPoints += score.Points;

                                    await SendPlayerScores(code, questionId);
                                }

                                await Clients.Caller.SendAsync("ReceiveAnswerResult", result);
                            }
                        }
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

        public static Player? FindPlayerById(string id)
        {
            return _rooms.SelectMany(room => room.Value.Players).FirstOrDefault(player => player.Id == id);
        }

        public static ScoreDTO ScoreToDTO(Score score) => new()
        {
            HasAnsweredRight = score.HasAnsweredRight,
            Points = score.Points,
            Order = score.Order
        };
    }
}
