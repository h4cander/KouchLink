using KouchLink.Common.Data;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using Newtonsoft.Json;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace KouchLink.Host
{
    internal class Program
    {
        private static ViGEmClient client;
        private static IXbox360Controller controller;

        static void Main(string[] args)
        {
            // 初始化虛擬手把
            client = new ViGEmClient();
            controller = client.CreateXbox360Controller();
            controller.Connect();

            var manager = new Xbox360ControllerManager(controller);

            Console.WriteLine("Host? eg.ws://localhost:5000/host or wss://localhost:5000/host");
            string serverUri = Console.ReadLine(); //"ws://localhost:5000/host"; // 指向你的 ASP.NET Core server

            ExecuteAsync(serverUri, manager).GetAwaiter().GetResult();
        }

        private static async Task ExecuteAsync(string serverUri, Xbox360ControllerManager manager)
        {
            using (ClientWebSocket ws = new ClientWebSocket())
            {
                try
                {
                    await ws.ConnectAsync(new Uri(serverUri), CancellationToken.None);

                    Console.WriteLine("Connected!");
                    
                    var buffer = new byte[1024];

                    try
                    {
                        while (ws.State == WebSocketState.Open)
                        {
                            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                Console.WriteLine("Server closed connection.");
                                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                            }
                            else
                            {
                                try
                                {
                                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                                    Console.WriteLine(message);
                                    var data = JsonConvert.DeserializeObject<JoystickData>(message);
                                    if (data != null)
                                    {
                                        manager.Update(data);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Error: " + ex.Message);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Receive error: " + ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }

                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
            }
        }

        private class Xbox360ControllerManager
        {
            private readonly IXbox360Controller controller;
            private readonly System.Timers.Timer timer;
            private DateTime lastReceived;
            private readonly int timeoutMs;

            public Xbox360ControllerManager(IXbox360Controller controller, int timeoutMilliseconds = 10)
            {
                this.controller = controller;
                this.timeoutMs = timeoutMilliseconds;
                lastReceived = DateTime.MinValue;

                timer = new System.Timers.Timer(1); // 每 1ms 檢查一次
                timer.AutoReset = true;
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
            }

            private void Timer_Elapsed(object sender, ElapsedEventArgs e)
            {
                if ((DateTime.Now - lastReceived).TotalMilliseconds > timeoutMs)
                {
                    ResetController();
                }
            }

            public void Update(JoystickData data)
            {
                lastReceived = DateTime.Now;

                // 更新搖桿軸
                controller.SetAxisValue(Xbox360Axis.LeftThumbX, data.axes.lx);
                controller.SetAxisValue(Xbox360Axis.LeftThumbY, data.axes.ly);
                controller.SetAxisValue(Xbox360Axis.RightThumbX, data.axes.rx);
                controller.SetAxisValue(Xbox360Axis.RightThumbY, data.axes.ry);

                // 更新按鈕
                controller.SetButtonState(Xbox360Button.A, data.buttons.A);
                controller.SetButtonState(Xbox360Button.B, data.buttons.B);
                controller.SetButtonState(Xbox360Button.X, data.buttons.X);
                controller.SetButtonState(Xbox360Button.Y, data.buttons.Y);
            }

            private void ResetController()
            {
                controller.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
                controller.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
                controller.SetAxisValue(Xbox360Axis.RightThumbX, 0);
                controller.SetAxisValue(Xbox360Axis.RightThumbY, 0);

                controller.SetButtonState(Xbox360Button.A, false);
                controller.SetButtonState(Xbox360Button.B, false);
                controller.SetButtonState(Xbox360Button.X, false);
                controller.SetButtonState(Xbox360Button.Y, false);
            }
        }
    }
}
