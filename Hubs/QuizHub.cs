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
        private static readonly int _roomQuestionsCount = 3;
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

            var randomQuestions = await _context.Questions
                .OrderBy(q => Guid.NewGuid())
                .Take(_roomQuestionsCount)
                .ToListAsync();

            foreach (var question in randomQuestions)
            {
                room.Questions.Add(question);
            }

            room.Players.Add(new Player {
                ConnectionId = Context.ConnectionId,
                Name = playerName,
                Room = room
            });

            _rooms.Add(room.Id, room);

            await Groups.AddToGroupAsync(Context.ConnectionId, room.Id.ToString());

            await SendMessage(room.Id, $"{playerName} created the room.");
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
                Console.WriteLine("join room");
            }
            else
            {
                await CreateRoom(playerName);
                Console.WriteLine("create room");
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

        public async Task SendMessage(long roomId, string message)
        {
            await Clients.Group(roomId.ToString()).SendAsync("ReceiveMessage", message);
        }

        public async Task GetQuestions(long roomId)
        {
            if (_rooms.TryGetValue(roomId, out Room? room))
            {
                await Clients.Group(roomId.ToString()).SendAsync("ReceiveQuestions", room.Questions);
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
