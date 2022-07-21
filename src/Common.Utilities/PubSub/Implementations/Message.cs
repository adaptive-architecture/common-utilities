using System;
using AdaptArch.Common.Utilities.PubSub.Contracts;

namespace AdaptArch.Common.Utilities.PubSub.Implementations;

public class MessageBuilder<T>: IMessageBuilder<T>
    where T : class
{
    public class Message<TData> : IMessage<TData> where TData : class
    {
        public Message(string id, DateTime timestamp, string topic, TData data)
        {
            Id = id;
            Timestamp = timestamp;
            Topic = topic;
            Data = data;
        }


        public string Id { get; }

        public string Topic { get; }

        public DateTime Timestamp { get; }

        public TData Data { get; }
    }

    public IMessage<T> Build(string topic, T data)
    {
        return new Message<T>(
            Guid.NewGuid().ToString("N"),
            DateTime.UtcNow,
            topic,
            data
        );
    }

}
