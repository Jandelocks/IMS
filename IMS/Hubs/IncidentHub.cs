using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

public class IncidentHub : Hub
{
    public async Task SendUpdate()
    {
        await Clients.All.SendAsync("ReceiveIncidentUpdate");
    }
}
