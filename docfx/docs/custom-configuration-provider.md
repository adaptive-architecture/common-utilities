# Custom Configuration Provider

Implement custom configuration providers without the complexity of understanding Microsoft.Extensions.Configuration internals.

## Overview

Custom configuration providers enable you to:

- ✅ **Load configuration from any source** (databases, APIs, custom files)
- ✅ **Integrate seamlessly** with the .NET configuration system
- ✅ **Support configuration reloading** and change notifications
- ✅ **Simplify implementation** using the built-in data provider abstraction

## Basic Usage

### Database Configuration Example

This example demonstrates loading configuration from a database table called `configuration_values`:

|key|value|modified_date|
|:--- |:--- | ---:|
|Enabled|1|2022-09-18T15:17:43Z|
|MaxThreads|10|2022-09-18T15:17:43Z|

Before you register a custom configuration provider you need to implement your own version of `AdaptArch.Common.Utilities.Configuration.Contracts.IDataProvider`.

``` csharp

public class DbConfigurationValuesDataProvider: IDataProvider
{
  private readonly string _connectionString;

  public DbConfigurationValuesDataProvider(string connectionString)
  {
    _connectionString = connectionString;
  }

  public async Task<IReadOnlyDictionary<string, string?>> ReadDataAsync(CancellationToken cancellationToken = default)
  {
    using (SqlConnection connection = new SqlConnection(_connectionString))
    {
      connection.Open();

      var configurationData = new Dictionary<string, string?>();

      using (var command = new SqlCommand("SELECT key, value FROM configuration_values", connection))
      {
        using (var reader = await command.ExecuteReaderAsync(cancellationToken)) ExecuteScalarAsync
        {
          while (reader.Read())
          {
            configurationData.Add(reader.GetString(0), reader.GetString(1));
          }
        }
      }

      return configurationData;
    }
  }

  public async Task<string> GetHashAsync(CancellationToken cancellationToken = default)
  {
    // The provider will use the maximum value of the "modified_date" column as a hash.
    // This will prevent the subsequent call to "ReadDataAsync" from happening as well as any possible object re-initialization
    // caused by a "false" configuration reload.
    using (var connection = new SqlConnection(_connectionString))
    {
      connection.Open();

      using (SqlCommand command = new SqlCommand("SELECT MAX(modified_date) FROM configuration_values", connection))
      {
        var lastModified = await command.ExecuteScalarAsync<DateTime>(cancellationToken);
        return lastModified.ToString("O");
      }
    }
  }
}

```

## Using the provider

Now that you have the configuration data provider created you need to:
* Define an options class to use the [options pattern](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-6.0).
* Register the provider in you application start-up.

``` csharp

// Class used to bind via the options pattern.
public class ParallelProcessingOptions
{
  public bool Enabled { get; set; }
  public int MaxThreads { get; set; }
}

// Extension method to make registration easier.
public static class ConfigurationBuilderExtensions
{
  public static IConfigurationBuilder AddDbConfiguration(this IConfigurationBuilder builder, string sectionName)
  {
    // Build a temporary configuration so you can read the necessary values.
    var tempConfig = builder.Build();
    var connectionString = tempConfig.GetConnectionString("ConfigurationDatabaseConnectionString");

    return builder.AddCustomConfiguration(source => {
      source.DataProvider = new DbConfigurationValuesDataProvider(connectionString);
      source.Options.PoolingInterval = TimeSpan.FromHours(24);
      source.Options.HandleLoadException = (ctx) => {

        Console.WriteLine(ctx.Exception);

        // In a real application you could ignore timeout exceptions and not stop the pooling but you might
        // want to stop pooling and not ignore if it's an authentication/authorization exception.

        return new LoadExceptionHandlerResult
        {
          // For this demo it is OK to ignore the error if this is happening while reloading the configuration.
          IgnoreException = ctx.Reload,
          // If you want to stop pooling for configuration changes in case of an error set this to true.
          DisablePooling = false
        };
      };

      source.Options.Prefix = sectionName;
    });
  }
}

const string configurationSectionName = "ParallelProcessing";
var host = Host.CreateDefaultBuilder(args)
  .ConfigureAppConfiguration((_, configuration) =>
  {
    // Register the configuration provider.
    configuration.AddDbConfiguration(configurationSectionName);
  })
  .ConfigureServices((context, services) =>
    // Bind the options.
    services.Configure<ParallelProcessingOptions>(context.Configuration.GetSection(configurationSectionName))
  )
  .Build();

await host.RunAsync();
```


To take advantage of the reload functionality of the configuration provider and have the latest configuration values you should not use an instance of `ParallelProcessingOptions` but instead use `IOptionsSnapshot<ParallelProcessingOptions>` or `IOptionsMonitor<ParallelProcessingOptions>`. For more details see the [official documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/options#options-interfaces).


## Advanced usage

Depending on the storage you will use you might have cases where your `IDataProvider` implementation will return the data in a more compact format:

|key|value|
|:--- |:--- |
|CentralConfiguration/SectionA|{ "foo": 1, "bar": 2, "buzz": 3}|
|CentralConfiguration/SectionB|{ "foo": { "bar": "buzz" }|

To allow the data provider to correctly parse the values you can change the following options:

``` csharp
// This will transform "CentralConfiguration/SectionA" to "CentralConfiguration:SectionA".
source.Options.OriginalKeyDelimiter = "/";
// This will go through the JSON properties and create a linear set ov values.
source.Options.ConfigurationParser = new JsonConfigurationParser(ConfigurationPath.KeyDelimiter);
```

With the above options added to the configuration the parsed values are:

|key|value|
|:--- |:--- |
|CentralConfiguration:SectionA:foo|1|
|CentralConfiguration:SectionA:bar|2|
|CentralConfiguration:SectionA:buzz|3|
|CentralConfiguration:SectionB:foo:bar|buzz|
