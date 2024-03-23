using System.Text;
using HuaJiBot.NET.Bot;
using PureNSQSharp;
using PureNSQSharp.Utils;

namespace HuaJiBot.NET.Plugin.RepairTeam;

public class NsqConnector : IDisposable
{
    private readonly Consumer _consumer;

    public NsqConnector(string url, string topicName, string channelName, string authSecret)
    {
        _consumer = new Consumer(
            topicName,
            channelName,
            new PureNSQSharp.Config { AuthSecret = authSecret }
        );
        _consumer.AddHandler(new MessageHandler());
        _consumer.ConnectToNSQd(url);
        Console.WriteLine($"Connected to {url} topic: {topicName} channel: {channelName}");
    }

    public void Dispose()
    {
        _consumer.StopAsync();
    }

    private class MessageHandler : IHandler
    {
        /// <summary>Handles a message.</summary>
        public void HandleMessage(IMessage message)
        {
            string msg = Encoding.UTF8.GetString(message.Body);
            Console.WriteLine(msg);
        }

        /// <summary>
        /// Called when a message has exceeded the specified <see cref="Config.MaxAttempts"/>.
        /// </summary>
        /// <param name="message">The failed message.</param>
        public void LogFailedMessage(IMessage message)
        {
            // Log failed messages
        }
    }
}
