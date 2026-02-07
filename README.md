# Tsukikage
Tsukikage is a program that reads the JSON output produced by [OwOCR](https://github.com/AuroraWright/owocr), which contains OCRed text and their coordinates, and sends the preferred output when the user hovers over the corresponding regions. See [Tsukikage.ini](https://github.com/rampaa/Tsukikage/blob/master/Tsukikage/Tsukikage.ini) for all available settings and output types.

Download from the [releases page](https://github.com/rampaa/Tsukikage/releases).

## System requirements
* .NET Desktop Runtime 10.0 or later

## How do I use Tsukikage?

### 1. Install and configure [OwOCR](https://github.com/AuroraWright/owocr)

To use Tsukikage, you first need to [install OwOCR](https://github.com/AuroraWright/owocr?tab=readme-ov-file#installation).  

#### Important OwOCR settings

1. **`read_from`**  
   Select `screencapture`.  
   Other options *may* work if the image's top-left position is at `x = 0, y = 0`, but using anything other than `screencapture` is **not recommended**.

2. **`write_to`**  
   If you intend to use Tsukikage with [JL](https://github.com/rampaa/JL), set this to `websocket`.

3. **`websocket_port`**  
   Make sure the port number matches the port used in Tsukikage's `OcrJsonInputWebSocketAddress` setting.  
   Both use port `7331` by default.

4. **`output_format`**  
   Set this to `json`.

5. **`engines`**  
   Only enable OCR engine(s) you actually use.  
   This greatly affects OwOCR's startup time.  
   I personally only have `OneOCR` enabled.

6. **`screen_capture_area`**  
   If you use [Magpie](https://github.com/Blinue/Magpie), select `window`.   
   If you don't use Magpie, Tsukikage should work with all options, but `window` is still **strongly recommended** for efficiency and accuracy reasons.

8. **`screen_capture_delay_seconds`**  
   If OwOCR's CPU usage is too high, try increasing this value.  
   A higher value reduces CPU usage but increases the delay before Tsukikage receives updated text, which hurts interactivity.

   - For `OneOCR`, a value between `1.5` and `3` seconds works well for me, but YMMV.
   - If you need a very large value to keep CPU usage reasonable, consider disabling automatic capture by setting this to `-1` and instead using the `screen_capture_combo` hotkey to trigger OCR manually.

9. **`join_lines`**  
   Enable this option.

10. **`join_paragraphs`**  
   Disable this option.

11. **`paragraph_separator`**  
    Setting this to `\n\n` is recommended.

12. **`reorder_text`**  
    Enabling this is recommended.

**Note:** You must start OwOCR **after** the window you want to OCR is already open.

---

### 2. Install and configure [JL](https://github.com/rampaa/JL)

If you intend to use Tsukikage with JL, you must also install JL.  

#### Important JL settings
JL v4.0.0+ includes predefined `Profile`s. Selecting the `Tsukikage` profile will currently configure the following settings automatically:

1. **Enable Tsukikage WebSocket text capture**  
   Enabled.

2. **Auto reconnect to Tsukikage WebSocket**  
   Enabled.

4. **Tsukikage WebSocket server address**  
   Set to `ws://127.0.0.1:8768`.  
   This must match Tsukikage's `OutputWebSocketAddress` setting.  
   If you need to use a different port, make sure to keep both settings in sync.

5. **Hide popups on text change**  
   Disabled.

6. **Don't capture identical text from Clipboard/WebSocket**  
   Enabled. (Optional)

7. **Enable clipboard text capture**  
   Disabled. (Optional)

8. **Hide all buttons when mouse is not over the title bar**  
   Enabled. (Optional)

9. **Text only visible on hover**  
   Enabled. (Optional)

10. **Change opacity on unhover**  
    Enabled. (Optional)

11. **Don't auto look up the first term on text change if Main Window is not minimized**  
    Disabled. (Optional)  
    It is still recommended to minimize JL's main window, as you won't need to interact with it while using Tsukikage.

#### Other JL settings that may be useful

1. **`Lookup requires Lookup Key press`**  
   This can be enabled if you want to require a key press to avoid accidental lookups of in-game text.

2. **Enable mining mode**  
   Even though it may not be obvious, you can still use the `"Enable mining mode" button` to enable mining mode.  
   Alternatively, you can use the `Mining mode` hotkey, or the `Automatically enable mining mode` and `Mining mode activation delay (in milliseconds)` settings to enable mining mode.

3. **Popup settings for vertical text received from Tsukikage**  
   The following settings behave the same as their non-vertical counterparts, but only apply to vertical text received from Tsukikage:
   - `X Offset (Vertical Text)`
   - `Y Offset (Vertical Text)`
   - `Popup position relative to cursor (Vertical Text)`
   - `Flip (Vertical Text)`


---

### 3. Important Tsukikage settings

All Tsukikage settings and their explanations can be found in the [Tsukikage.ini](https://github.com/rampaa/Tsukikage/blob/master/Tsukikage/Tsukikage.ini) file.

1. **`OcrJsonInputWebSocketAddress`**  
   Must use the same port as OwOCR's `websocket_port`.  
   Both default to `7331`.

2. **`TextHookerWebSocketAddress`**  
   WebSocket address of a text hooker.  
   Text received from this address is used to correct OCR mistakes when possible.  
   Leave this empty or commented out if you cannot hook the source of the OCRed image.

3. **`OutputType`**  
   If you intend to use Tsukikage with JL, set this to either `GraphemeInfo` or `TextStartingFromPosition`.

   - `GraphemeInfo` is **recommended**, as it provides metadata that allows JL to:
     - Correctly detect text orientation
     - Apply JL's vertical-text popup settings (`X Offset (Vertical Text)`, `Y Offset (Vertical Text)`, `Popup position (Vertical Text)`, `Flip (Vertical Text)`) when the text is vertical
     - Keep more accurate statistics when possible

4. **`OutputIpcMethod`**  
   If you intend to use Tsukikage with JL, set this to `WebSocket`.

5. **`OutputWebSocketAddress`**  
   Must match JL's **Tsukikage WebSocket server address**.  
   If you need to use a different port, make sure to keep both settings in sync.

## License
Licensed under the GPL-2.0 only
