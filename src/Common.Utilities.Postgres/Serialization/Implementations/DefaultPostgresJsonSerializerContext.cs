using System.Text.Json.Serialization;

namespace AdaptArch.Common.Utilities.Postgres.Serialization.Implementations;

/// <summary>
/// Default JSON serializer context for PostgreSQL leader election metadata serialization.
/// </summary>
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(IReadOnlyDictionary<string, string>))]
public partial class DefaultPostgresJsonSerializerContext : JsonSerializerContext;
