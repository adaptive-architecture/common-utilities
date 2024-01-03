using AdaptArch.Common.Utilities.Configuration.Contracts;
using Microsoft.Extensions.Configuration;

namespace AdaptArch.Common.Utilities.Configuration.Providers;

/// <inheritdoc />
public class CustomConfigurationProvider : ConfigurationProvider
{
    private readonly IDataProvider _dataProvider;
    private readonly CustomConfigurationProviderOptions _options;
    private readonly object _poolingStateLock = new();
    private readonly SemaphoreSlim _loadingStateSemaphore = new(1, 1);

    private string _dataHash = String.Empty;
    private Task? _pollingTask;
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="dataProvider">The raw data provider.</param>
    /// <param name="options">The configuration provider options.</param>
    public CustomConfigurationProvider(IDataProvider dataProvider, CustomConfigurationProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(dataProvider);
        ArgumentNullException.ThrowIfNull(options);

        _dataProvider = dataProvider;
        _options = options;
    }

    /// <inheritdoc />
    public override void Load()
    {
        // At the moment we are ok with this hack since it should not be used in any "HOT PATH" and should not rely on UI or user context interaction.
        // Read more https://docs.microsoft.com/en-us/archive/msdn-magazine/2015/july/async-programming-brownfield-async-development#the-thread-pool-hack
        // Also we might not want to capture the caller context by using `ThreadPool.UnsafeQueueUserWorkItem` or `using (ExecutionContext.SuppressFlow()) { }`.

        Task.Run(() => LoadAsyncCore(false, CancellationToken.None)).GetAwaiter().GetResult();
    }

    private async Task LoadAsyncCore(bool reload, CancellationToken cancellationToken)
    {
        try
        {
            await _loadingStateSemaphore.WaitAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
            var hash = await _dataProvider.GetHashAsync(cancellationToken).ConfigureAwait(false);
            if (String.Equals(hash, _dataHash))
                return;

            var data = await _dataProvider.ReadDataAsync(cancellationToken).ConfigureAwait(false);
            Data = ProcessData(data);
            _dataHash = hash;

            OnReload();
            EnablePooling();
        }
        catch (Exception e)
        {
            var ignoreException = false;
            if (_options.HandleLoadException != null)
            {
                var result = _options.HandleLoadException(new LoadExceptionContext(e) { Reload = reload });
                ignoreException = result.IgnoreException;
                if (result.DisablePooling)
                {
                    DisablePooling();
                }
            }

            if (!ignoreException)
                throw;
        }
        finally
        {
            _loadingStateSemaphore.Release();
        }
    }

    private Dictionary<string, string?> ProcessData(IReadOnlyDictionary<string, string?> rawData)
    {
        Func<string, string> keyDelimiterReplacer = _options.OriginalKeyDelimiter == null
            ? k => k
            : k => k.Replace(_options.OriginalKeyDelimiter, ConfigurationPath.KeyDelimiter);

        Func<string, string> keyTransform = String.IsNullOrEmpty(_options.Prefix)
            ? k => keyDelimiterReplacer(k)
            : k => _options.Prefix + ConfigurationPath.KeyDelimiter + keyDelimiterReplacer(k);

        if (_options.ConfigurationParser == null)
        {
            return rawData.ToDictionary(k => keyTransform(k.Key), v => v.Value, StringComparer.InvariantCultureIgnoreCase);
        }

        var data = new Dictionary<string, string?>(StringComparer.InvariantCultureIgnoreCase);
        foreach (var kvp in rawData)
        {
            var newKey = keyTransform(kvp.Key);
            if (kvp.Value == null)
            {
                data.Add(newKey, kvp.Value);
            }
            else
            {
                foreach (var parsed in _options.ConfigurationParser.Parse(kvp.Value))
                {
                    data.Add(newKey + ConfigurationPath.KeyDelimiter + parsed.Key, parsed.Value);
                }
            }
        }

        return data;
    }

    private void EnablePooling()
    {
        lock (_poolingStateLock)
        {
            if (_options.PoolingInterval <= TimeSpan.Zero || _pollingTask != null)
                return;

            _cancellationTokenSource = new CancellationTokenSource();
            _pollingTask = PollForChangesAsync(_cancellationTokenSource.Token);
        }
    }

    private void DisablePooling()
    {
        lock (_poolingStateLock)
        {
            if (_cancellationTokenSource == null)
                return;

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            _pollingTask = null;
        }
    }

    private async Task PollForChangesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.PoolingInterval, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
                await LoadAsyncCore(true, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Ignore all exceptions.
            }
        }
    }
}
