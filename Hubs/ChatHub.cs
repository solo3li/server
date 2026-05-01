using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Uis.Server.Hubs;

public class ChatHub : Hub
{
    public async Task SendMessage(string chatId, string message)
    {
        await Clients.Group(chatId).SendAsync("ReceiveMessage", Context.ConnectionId, message);
    }

    public async Task JoinChat(string chatId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
    }

    public async Task LeaveChat(string chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId);
    }
}
