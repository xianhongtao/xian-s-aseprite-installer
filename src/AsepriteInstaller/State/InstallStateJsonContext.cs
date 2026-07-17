using System.Text.Json;
using System.Text.Json.Serialization;

namespace AsepriteInstaller.State;

/// <summary>
/// JSON serializer context for Native AOT compatibility.
/// Source-generated serialization avoids reflection-based dynamic code.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(InstallState))]
[JsonSerializable(typeof(StepRecord))]
[JsonSerializable(typeof(List<StepRecord>))]
public sealed partial class InstallStateJsonContext : JsonSerializerContext
{
}
