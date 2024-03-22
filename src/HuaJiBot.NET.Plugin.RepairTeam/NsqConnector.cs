using System.Text;
using HuaJiBot.NET.Bot;
using PureNSQSharp;

namespace HuaJiBot.NET.Plugin.RepairTeam;

internal class NsqConnector : IDisposable
{
    private readonly Consumer _consumer;

    public NsqConnector(string url, string topicName, string channelName)
    {
        _consumer = new Consumer("test-topic-name", "channel-name");
        _consumer.AddHandler(new MessageHandler());
        _consumer.ConnectToNSQd("127.0.0.1:4161");
        Console.WriteLine(
            "Listening for messages. If this is the first execution, it "
                + "could take up to 60s for topic producers to be discovered."
        );
    }

    public void Dispose()
    {
        _consumer.Stop();
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
