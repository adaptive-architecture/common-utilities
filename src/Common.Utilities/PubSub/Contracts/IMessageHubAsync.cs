namespace AdaptArch.Common.Utilities.PubSub.Contracts;

public interface IMessageHubAsync
{
    Task PublishAsync<TMessageData>(string topic, TMessageData data, CancellationToken cancellationToken)
        where TMessageData : class;

    Task<string> SubscribeAsync<TMessageData>(string topic, MessageHandler<TMessageData> handler, CancellationToken cancellationToken)
        where TMessageData : class;

    Task UnsubscribeAsync(string id, CancellationToken cancellationToken);
}
