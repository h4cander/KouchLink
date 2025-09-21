using Microsoft.AspNetCore.Builder;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KouchLink.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();

            app.UseStaticFiles();

            app.UseWebSockets();

            // �s��w�s�u�� Host
            WebSocket? hostSocket = null;

            // Host �ݳs�u�]Host WebSocket �]�s��o�̡^
            app.Map("/host", async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    hostSocket = await context.WebSockets.AcceptWebSocketAsync();
                    Console.WriteLine("Host connected");
                    await ReceiveLoop(hostSocket);
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            });

            // Client (P2) �s�u
            app.Map("/client", async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    using var clientSocket = await context.WebSockets.AcceptWebSocketAsync();
                    Console.WriteLine("Client connected");
                    await ReceiveLoop(clientSocket, hostSocket);
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            });

            app.Run();
        }

        static async Task ReceiveLoop(WebSocket source, WebSocket? target = null)
        {
            var buffer = new byte[1024];

            while (source.State == WebSocketState.Open)
            {
                var result = await source.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    Console.WriteLine(message);

                    // �p�G target �s�b�B�}�ҡA��o�T��
                    if (target != null && target.State == WebSocketState.Open)
                    {
                        await target.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)),
                            WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await source.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
        }
    }
}
