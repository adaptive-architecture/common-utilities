namespace AdaptArch.Common.Utilities.PubSub.Contracts;

public interface IMessageHub
{
    void Publish<TMessageData>(string topic, TMessageData data)
        where TMessageData : class;

    string Subscribe<TMessageData>(string topic, MessageHandler<TMessageData> handler)
        where TMessageData : class;

    void Unsubscribe(string id);
}
