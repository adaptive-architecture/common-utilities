using System.Text.Json.Serialization;

namespace AdaptArch.Common.Utilities.AspNetCore.Middlewares.VersionedStaticFiles;

[JsonSerializable(typeof(VersionFilePayload))]
internal partial class DefaultJsonSerializerContext : JsonSerializerContext;
