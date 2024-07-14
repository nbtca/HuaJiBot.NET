using System.Text;
using PureNSQSharp;

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
        _consumer.AddHandler(
            new MessageHandler(msg =>
            {
                MessageReceived?.Invoke(this, msg);
            })
        );
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    _consumer.ConnectToNSQd(url);
                    Console.WriteLine(
                        $"Connected to {url} topic: {topicName} channel: {channelName}"
                    );
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to connect to NSQd, retrying in 10 seconds.");
                    Console.WriteLine(e);
                    await Task.Delay(10000);
                }
            }
        });
    }

    public event EventHandler<string>? MessageReceived;

    public void Dispose()
    {
        _consumer.StopAsync();
        GC.SuppressFinalize(this);
    }

    private class MessageHandler(Action<string> onMsg) : IHandler
    {
        /// <summary>Handles a message.</summary>
        public void HandleMessage(IMessage message)
        {
            var msg = Encoding.UTF8.GetString(message.Body);
            onMsg(msg);
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
