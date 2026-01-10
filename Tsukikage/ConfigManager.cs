using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SharpConfig;
using Tsukikage.OCR;
using Tsukikage.Utilities;

namespace Tsukikage;

internal static class ConfigManager
{
    #region IniFile
    private const string InputSection = "Input";
    private const string OutputSection = "Output";

    private const string OcrJsonInputWebSocketAddressComment =
        """
        ; WebSocket address where the program receives OCR results from OwOCR in JSON format.
        ; Default value: ws://127.0.0.1:7331
        """;

    private const string TextHookerWebSocketAddressComment =
        """
        ; WebSocket address of a text hooker (e.g., Textractor).
        ; Text received from this address will be used to correct OCR mistakes where possible.
        ; Leave empty or comment out if not used.
        ; Default value: Empty
        """;

    private const string OutputTypeComment =
        """
        ; Specifies the type of output the program sends.
        ; Default value: GraphemeInfo
        ; Possible values and their meaning:
        ; GraphemeInfo             : JSON containing the start index of the current grapheme within the current paragraph, the current paragraph, and the text direction
        ; Paragraph                : The paragraph at the current mouse position
        ; TextStartingFromPosition : Text starting from the current mouse position to the end of the current paragraph
        ; Line                     : The line at the current mouse position
        ; Word                     : The word at the current mouse position
        ; Grapheme                 : The grapheme at the current mouse position
        """;

    private const string OutputDelayInMillisecondsComment =
        """
        ; Time in milliseconds that the output must remain unchanged before it is sent.
        ; Default value: 0
        """;

    private const string OutputIpcMethodComment =
        """
        ; Specifies the method used to send output.
        ; Default value: WebSocket
        ; Possible values:
        ; WebSocket : Send output via WebSocket
        ; Clipboard : Copy output to the system clipboard
        """;

    private const string OutputWebSocketAddressComment =
        """
        ; WebSocket address where output is sent if OutputIpcMethod is WebSocket.
        ; Default value: ws://127.0.0.1:8765
        """;

    private const string IniFileContent =
        $"""
        [{InputSection}]
        {OcrJsonInputWebSocketAddressComment}
        {nameof(OcrJsonInputWebSocketAddress)} = {DefaultOcrJsonInputWebSocketAddress}

        {TextHookerWebSocketAddressComment}
        ; {nameof(TextHookerWebSocketAddress)} = ws://127.0.0.1:6677

        [{OutputSection}]
        {OutputTypeComment}
        {nameof(OutputType)} = GraphemeInfo

        {OutputDelayInMillisecondsComment}
        {nameof(OutputDelayInMilliseconds)} = 0

        {OutputIpcMethodComment}
        {nameof(OutputIpcMethod)} = WebSocket

        {OutputWebSocketAddressComment}
        {nameof(OutputWebSocketAddress)} = {DefaultOutputWebSocketAddress}
        """;

    #endregion

    private const string DefaultOcrJsonInputWebSocketAddress = "ws://127.0.0.1:7331";
    private const string DefaultOutputWebSocketAddress = "ws://127.0.0.1:8765";

    public static Uri OcrJsonInputWebSocketAddress { get; private set; } = new(DefaultOcrJsonInputWebSocketAddress, UriKind.Absolute);
    public static Uri? TextHookerWebSocketAddress { get; private set; } // = null;
    public static OutputPayload OutputType { get; private set; } = OutputPayload.GraphemeInfo;
    public static double OutputDelayInMilliseconds { get; private set; } // = 0;
    public static OutputIpcMethod OutputIpcMethod { get; private set; } = OutputIpcMethod.WebSocket;
    public static Uri OutputWebSocketAddress { get; private set; } = new(DefaultOutputWebSocketAddress, UriKind.Absolute);

    public static void Load()
    {
        if (!File.Exists(AppInfo.ConfigFilePath))
        {
            File.WriteAllText(AppInfo.ConfigFilePath, IniFileContent);
            return;
        }

        Configuration.PreferredCommentChar = ';';
        Configuration.IgnorePreComments = false;
        Configuration.IgnoreInlineComments = false;
        Configuration config = Configuration.LoadFromFile(AppInfo.ConfigFilePath);

        Section inputSection = config["Input"];
        OcrJsonInputWebSocketAddress = GetWebSocketConfigValue(nameof(OcrJsonInputWebSocketAddress), OcrJsonInputWebSocketAddress, OcrJsonInputWebSocketAddressComment, inputSection);
        TextHookerWebSocketAddress = GetWebSocketConfigValue(nameof(TextHookerWebSocketAddress), TextHookerWebSocketAddress, TextHookerWebSocketAddressComment, inputSection, false);

        Section outputSection = config["Output"];
        OutputType = GetEnumConfigValue(nameof(OutputType), OutputType, OutputTypeComment, outputSection);
        OutputDelayInMilliseconds = GetConfigValue(nameof(OutputDelayInMilliseconds), OutputDelayInMilliseconds, OutputDelayInMillisecondsComment, outputSection);
        OutputIpcMethod = GetEnumConfigValue(nameof(OutputIpcMethod), OutputIpcMethod, OutputIpcMethodComment, outputSection);
        OutputWebSocketAddress = GetWebSocketConfigValue(nameof(OutputWebSocketAddress), OutputWebSocketAddress, OutputWebSocketAddressComment, outputSection);

        string tempConfigFilePath = PathUtils.GetTempPath(AppInfo.ConfigFilePath);
        config.SaveToFile(tempConfigFilePath);
        PathUtils.ReplaceFileAtomicallyOnSameVolume(AppInfo.ConfigFilePath, tempConfigFilePath);
    }

    [return: NotNullIfNotNull(nameof(defaultValue))]
    private static Uri? GetWebSocketConfigValue(string key, Uri? defaultValue, string defaultComment, Section section, bool writeToFileIfMissing = true)
    {
        int settingCount = section.SettingCount;

        Setting setting = section[key];
        string valueFromConfig = setting.RawValue;

        // If setting count increased that means the key did not exist before
        if (section.SettingCount > settingCount)
        {
            if (writeToFileIfMissing)
            {
                setting.RawValue = defaultValue?.OriginalString ?? "";
                setting.Comment = defaultComment;
            }
            else
            {
                _ = section.Remove(key);
            }

            return defaultValue;
        }

        if (Uri.TryCreate(valueFromConfig, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeWs || uri.Scheme == Uri.UriSchemeWss))
        {
            return uri;
        }

        setting.RawValue = defaultValue?.OriginalString ?? "";
        return defaultValue;
    }

    private static T GetEnumConfigValue<T>(string key, T defaultValue, string defaultComment, Section section) where T : struct, Enum
    {
        int settingCount = section.SettingCount;

        Setting setting = section[key];
        string valueFromConfig = setting.RawValue;

        // If setting count increased that means the key did not exist before
        if (section.SettingCount > settingCount)
        {
            setting.RawValue = defaultValue.ToString();
            setting.Comment = defaultComment;
            return defaultValue;
        }

        if (Enum.TryParse(valueFromConfig, true, out T value))
        {
            return value;
        }

        setting.RawValue = defaultValue.ToString();
        return defaultValue;
    }

    private static T GetConfigValue<T>(string key, T defaultValue, string defaultComment, Section section) where T : struct, IConvertible, IParsable<T>
    {
        int settingCount = section.SettingCount;

        Setting setting = section[key];
        string valueFromConfig = setting.RawValue;

        // If setting count increased that means the key did not exist before
        if (section.SettingCount > settingCount)
        {
            setting.RawValue = defaultValue.ToString();
            setting.Comment = defaultComment;
            return defaultValue;
        }

        if (T.TryParse(valueFromConfig, CultureInfo.InvariantCulture, out T value))
        {
            return value;
        }

        setting.RawValue = defaultValue.ToString();
        return defaultValue;
    }

    public static string CurrentConfigString()
    {
        return
            $"""
            Current configs:
            OCR JSON Input WebSocket Address: {OcrJsonInputWebSocketAddress.OriginalString}
            Text hooker WebSocket AAddress: {TextHookerWebSocketAddress?.OriginalString ?? "Disabled"}
            Output Type: {OutputType}
            Output delay (in milliseconds): {OutputDelayInMilliseconds}
            Output Method: {OutputIpcMethod}
            Output WebSocket Address: {DefaultOutputWebSocketAddress}
            """;
    }
}
