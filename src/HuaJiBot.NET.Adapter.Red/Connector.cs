namespace HuaJiBot.NET.Adapter.Red;

internal class Connector
{
    public void Connect()
    {
        Websocket.Client.WebsocketClient client = new("ws://localhost:8080");
        Console.WriteLine("Connect to Red");
    }
}
