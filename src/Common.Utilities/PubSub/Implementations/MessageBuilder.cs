using AdaptArch.Common.Utilities.GlobalAbstractions.Contracts;
using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.PubSub.Implementations.Internals;

namespace AdaptArch.Common.Utilities.PubSub.Implementations;

/// <inheritdoc />
public class MessageBuilder<T>: IMessageBuilder<T>
    where T : class
{

    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUuidProvider _uuidProvider;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="dateTimeProvider">A date time provider.</param>
    /// <param name="uuidProvider">An UUID provider.</param>
    public MessageBuilder(IDateTimeProvider dateTimeProvider, IUuidProvider uuidProvider)
    {
        _dateTimeProvider = dateTimeProvider;
        _uuidProvider = uuidProvider;
    }

    /// <inheritdoc />
    public IMessage<T> Build(string topic, T data)
    {
        return new Message<T>(
            _uuidProvider.New(),
            _dateTimeProvider.UtcNow,
            topic,
            data
        );
    }
}
