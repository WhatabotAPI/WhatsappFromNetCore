using Newtonsoft.Json.Linq;
using System.Net.WebSockets;
using System.Text;

namespace WhatabotRealtimeClient
{
    class Program
    {
        private string ApiKey = "YOUR_API_KEY";
        private string ChatId = "YOUR_PHONE_NUMBER";

        static async Task Main(string[] args)
        {
            Program prg = new Program();
            await prg.RunWebSocket();
        }

        private async Task RunWebSocket()
        {
            var url = new Uri("wss://api.whatabot.io/realtimeMessages");

            while (true)
            {
                try
                {
                    using (var ws = new ClientWebSocket())
                    {
                        ws.Options.SetRequestHeader("x-api-key", ApiKey);
                        ws.Options.SetRequestHeader("x-chat-id", ChatId);
                        ws.Options.SetRequestHeader("x-platform", "whatsapp");

                        await ws.ConnectAsync(url, CancellationToken.None);

                        string connectMessage = "{\"protocol\":\"json\",\"version\":1}\u001e";
                        await SendMessageAsync(ws, connectMessage);

                        Console.WriteLine("Connected");

                        await ReceiveMessages(ws);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: " + ex.ToString());
                }

                Console.WriteLine("Attempting to reconnect...");
                await Task.Delay(20000);
            }
        }

        private async Task ReceiveMessages(ClientWebSocket ws)
        {
            var receiveBuffer = new byte[1024];
            while (ws.State == WebSocketState.Open)
            {
                try
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                    var message = System.Text.Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
                    try
                    {
                        message = message.TrimEnd('\u001e');
                        var jsonMessage = JObject.Parse(message);
                        var argumentsArray = jsonMessage["arguments"];
                        var messageTarget = jsonMessage["target"];


                        if (messageTarget != null && messageTarget.ToString() == "ReceiveMessage")
                        {
                            if (argumentsArray != null && argumentsArray.HasValues && argumentsArray.First is JValue argumentValue)
                            {
                                var textInsideArguments = argumentValue.Value?.ToString();
                                if (textInsideArguments != null && textInsideArguments != string.Empty)
                                {
                                    //we could implement a logic for message detection:
                                    // switch (textInsideArguments)
                                    // {
                                    //     case "start":
                                    //         Console.WriteLine("Starting the process...");
                                    //         break;
                                    //     case "stop":
                                    //         Console.WriteLine("Stopping the process...");
                                    //         break;
                                    //     case "pause":
                                    //         Console.WriteLine("Pausing the process...");
                                    //         break;
                                    //     case "resume":
                                    //         Console.WriteLine("Resuming the process...");
                                    //         break;
                                    //     default:
                                    //         Console.WriteLine("Unknown command");
                                    //         break;
                                    // }

                                    //Here I send a WhatsApp message with the text: "Pong: [message received by whatabot]  
                                    var responseMessage = "{\"type\":1,\"target\":\"SendMessage\",\"arguments\":[\"" + textInsideArguments + "\"]}";
                                    await SendMessageAsync(ws, "Pong: " + responseMessage);
                                    Console.WriteLine("Message sent: " + "Pong: " + textInsideArguments);
                                }
                            }
                        }
                    }
                    catch (Newtonsoft.Json.JsonReaderException)
                    {
                        Console.WriteLine("Error parsing the message");
                    }
                }
                catch (WebSocketException ex)
                {
                    Console.WriteLine("WebSocket exception: " + ex.Message);
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }

        private async Task SendMessageAsync(ClientWebSocket ws, string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
