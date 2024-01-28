using Microsoft.AspNetCore.SignalR;
using QuizApi.Models;

namespace QuizApi.Hubs
{
    public class QuizHub : Hub
    {
        private readonly Dictionary<long, Room> _rooms = [];

        public async Task CreateRoom(string playerName)
        {
            var room = new Room();

            room.Players.Add(new Player {
                ConnectionId = Context.ConnectionId,
                Name = playerName
            });

            _rooms.Add(room.Id, room);

            await Groups.AddToGroupAsync(Context.ConnectionId, room.Id.ToString());

            await Clients.Caller.SendAsync("RoomCreated", room);
        }

        public async Task JoinRoom(long roomId, string playerName)
        {
            if (_rooms.TryGetValue(roomId, out Room? room))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, room.Id.ToString());

                room.Players.Add(new Player {
                    ConnectionId = Context.ConnectionId,
                    Name = playerName
                });

                await Clients.Caller.SendAsync("RoomJoined", room);
            }
            else
            {
                await CreateRoom(playerName);
            }
        }

        // public async Task SendMessage(string message)
        // {
        //     await Clients.All.SendAsync("ReceiveMessage", message);
        // }

        public async Task SendMessage(long roomId, string message)
        {
            await Clients.Group(roomId.ToString()).SendAsync("ReceiveMessage", message);
        }
    }
}
