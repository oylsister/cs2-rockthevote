using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace cs2_rockthevote
{
    public enum ChangeModeEnum
    {
        EndOfMap,
        Instant,
        RoundEnd
    }

    public class ChangeModeEnumConverter : JsonConverter<ChangeModeEnum>
{
    public override ChangeModeEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string value = reader.GetString() ?? "";
        return value switch
        {
            "Instant" => ChangeModeEnum.Instant,
            "RoundEnd" => ChangeModeEnum.RoundEnd,
            "EndOfMap" => ChangeModeEnum.EndOfMap,
            _ => throw new JsonException($"Unexpected value '{value}'")
        };
    }

    public override void Write(Utf8JsonWriter writer, ChangeModeEnum value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case ChangeModeEnum.Instant:
                writer.WriteStringValue("Instant");
                break;
            case ChangeModeEnum.RoundEnd:
                writer.WriteStringValue("RoundEnd");
                break;
            case ChangeModeEnum.EndOfMap:
                writer.WriteStringValue("EndOfMap");
                break;
            default:
                throw new JsonException($"Unexpected value '{value}'");
        }
    }
}
}
