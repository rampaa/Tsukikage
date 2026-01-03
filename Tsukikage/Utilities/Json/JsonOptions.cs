using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tsukikage.OCR.OwOCR;

namespace Tsukikage.Utilities.Json;

internal static class JsonOptions
{
    internal static readonly JsonSerializerOptions s_jsoWithEnumConverter = new()
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters =
        {
            new JsonStringEnumConverter<OwocrWritingDirection>()
        }
    };
}
