using System.Text.Json.Serialization;
using System.Text.Json;

namespace HyacineProxy;

#if NET8_0_OR_GREATER
[JsonSourceGenerationOptions(
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip
)]
[JsonSerializable(typeof(Config))]
internal partial class ConfigContext : JsonSerializerContext
{
}
#endif