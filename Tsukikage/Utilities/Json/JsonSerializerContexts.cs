using System.Text.Json;
using System.Text.Json.Serialization;
using Tsukikage.OCR;
using Tsukikage.OCR.OwOCR;

namespace Tsukikage.Utilities.Json;

[JsonSerializable(typeof(OcrResult))]
[JsonSerializable(typeof(WordInfo))]
internal sealed partial class OcrResultJsonContext : JsonSerializerContext
{
    static OcrResultJsonContext()
    {
        Default = new OcrResultJsonContext(CreateJsonSerializerOptions(Default));
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions(OcrResultJsonContext defaultContext)
    {
        return JsonOptions.s_jsoWithEnumConverter;
    }
}
