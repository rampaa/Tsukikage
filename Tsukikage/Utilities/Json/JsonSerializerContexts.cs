using System.Text.Json;
using System.Text.Json.Serialization;
using Tsukikage.OCR;
using Tsukikage.OCR.OwOCR;

namespace Tsukikage.Utilities.Json;

[JsonSerializable(typeof(OwocrOcrResult))]
[JsonSerializable(typeof(GraphemeInfo))]
internal sealed partial class OcrResultJsonContext : JsonSerializerContext
{
    static OcrResultJsonContext()
    {
        Default = new OcrResultJsonContext(CreateJsonSerializerOptions());
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        return JsonOptions.s_jsoWithEnumConverter;
    }
}
