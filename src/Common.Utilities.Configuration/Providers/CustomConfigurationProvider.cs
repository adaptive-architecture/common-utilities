using AdaptArch.Common.Utilities.Configuration.Contracts;
using Microsoft.Extensions.Configuration;

namespace AdaptArch.Common.Utilities.Configuration.Providers;

/// <inheritdoc />
public class CustomConfigurationProvider : ConfigurationProvider
{
    private readonly IDataProvider _dataProvider;
    private readonly CustomConfigurationProviderOptions _options;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="dataProvider">The raw data provider.</param>
    /// <param name="options">The configuration provider options.</param>
    public CustomConfigurationProvider(IDataProvider dataProvider, CustomConfigurationProviderOptions options)
    {
        _dataProvider = dataProvider;
        _options = options;
    }

    /// <inheritdoc />
    public override void Load()
    {
        // At the moment we are ok with this hack since it should not be used in any "HOT PATH" and should not rely on UI or user context interaction.
        // Read more https://docs.microsoft.com/en-us/archive/msdn-magazine/2015/july/async-programming-brownfield-async-development#the-thread-pool-hack
        // Also we might not want to capture the caller context by using `ThreadPool.UnsafeQueueUserWorkItem` or `using (ExecutionContext.SuppressFlow()) { }`.

        Task.Run(LoadAsyncCore).GetAwaiter().GetResult();
    }

    private Task LoadAsyncCore()
    {
        return Task.CompletedTask;
    }
}
