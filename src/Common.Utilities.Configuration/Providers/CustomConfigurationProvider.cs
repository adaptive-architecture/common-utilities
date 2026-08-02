using AdaptArch.Common.Utilities.Configuration.Contracts;
using Microsoft.Extensions.Configuration;
using TaskExtensions = AdaptArch.Common.Utilities.Extensions.TaskExtensions;

namespace AdaptArch.Common.Utilities.Configuration.Providers;

/// <inheritdoc />
public class CustomConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly IDataProvider _dataProvider;
    private readonly CustomConfigurationProviderOptions _options;
    private readonly Lock _poolingStateLock = new();
    private readonly SemaphoreSlim _loadingStateSemaphore = new(1, 1);

    private string _dataHash = String.Empty;
    private Task? _pollingTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;

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
        TaskExtensions.RunSync(async delegate { await LoadAsyncCore(false, CancellationToken.None); }, CancellationToken.None);
    }

    /// <summary>
    /// Gets the data provider used by this configuration provider.
    /// </summary>
    public IDataProvider GetDataProvider() => _dataProvider;

    private async Task LoadAsyncCore(bool reload, CancellationToken cancellationToken)
    {
        try
        {
            await _loadingStateSemaphore.WaitAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
            var hash = await _dataProvider.GetHashAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
            if (String.Equals(hash, _dataHash))
                return;

            var data = await _dataProvider.ReadDataAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
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
            _ = _loadingStateSemaphore.Release();
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
            if (_disposed || _options.PoolingInterval <= TimeSpan.Zero || _pollingTask != null)
                return;

            _cancellationTokenSource = new CancellationTokenSource();
            _pollingTask = PollForChangesAsync(_cancellationTokenSource.Token);

            // Only clear the reference once the loop has actually stopped, so a subsequent
            // EnablePooling can never start a second poller alongside one that is still winding down.
            _ = _pollingTask.ContinueWith(_ =>
            {
                lock (_poolingStateLock)
                {
                    _pollingTask = null;
                }
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
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
            // _pollingTask is cleared by the continuation set up in EnablePooling once the
            // in-flight loop observes the cancellation and completes.
        }
    }

    private async Task PollForChangesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.PoolingInterval, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
                await LoadAsyncCore(true, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.None | ConfigureAwaitOptions.ForceYielding);
            }
            catch
            {
                // Ignore all exceptions.
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the resources used by the provider, stopping the background polling loop.
    /// </summary>
    /// <param name="disposing">true to release managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || !disposing)
        {
            return;
        }

        lock (_poolingStateLock)
        {
            _disposed = true;
            // ConfigurationRoot disposes providers that implement IDisposable; without this,
            // the CTS and the polling loop would outlive the provider forever.
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        _loadingStateSemaphore.Dispose();
    }
}
