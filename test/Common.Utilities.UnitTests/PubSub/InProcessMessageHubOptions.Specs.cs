using AdaptArch.Common.Utilities.PubSub.Contracts;
using AdaptArch.Common.Utilities.PubSub.Implementations;

namespace AdaptArch.Common.Utilities.UnitTests.PubSub;

public class InProcessMessageHubOptionsSpecs
{
    [Fact]
    public void OnMessageHandlerError_Should_Be_Null_By_Default()
    {
        var opt = new InProcessMessageHubOptions();
        Assert.Null(opt.OnMessageHandlerError);
    }

    [Fact]
    public void OnMessageHandlerError_Should_Be_Assignable()
    {
        // ReSharper disable once ConvertToLocalFunction
        Action<Exception, IMessage<object>> handler = (_, _) => { };
        var opt = new InProcessMessageHubOptions {OnMessageHandlerError = handler};

        Assert.Same(handler, opt.OnMessageHandlerError);
    }

    [Fact]
    public void GetMessageBuilder_Should_Not_Return_Null_By_Default()
    {
        var opt = new InProcessMessageHubOptions();
        Assert.NotNull(opt.GetMessageBuilder<object>());
    }

    [Fact]
    public void MaxDegreeOfParallelism_Should_Equal_Processor_Count()
    {
        var opt = new InProcessMessageHubOptions();
        Assert.Equal(Environment.ProcessorCount, opt.MaxDegreeOfParallelism);
    }

    [Fact]
    public void MaxDegreeOfParallelism_Should_Be_Assignable()
    {
        var opt = new InProcessMessageHubOptions
        {
            MaxDegreeOfParallelism = 100
        };
        Assert.Equal(100, opt.MaxDegreeOfParallelism);
    }
}
