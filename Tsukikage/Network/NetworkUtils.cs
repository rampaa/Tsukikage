using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using Tsukikage.Interop;

namespace Tsukikage.Network;

internal static class NetworkUtils
{
    private static readonly HttpClient s_client = new(new HttpClientHandler
    {
        UseProxy = false,
        CheckCertificateRevocationList = true
    });

    private static readonly Uri s_gitHubApiUrlForLatestTsukikageRelease = new("https://api.github.com/repos/rampaa/Tsukikage/releases/latest");

    public static async Task CheckAndInstallTsukikageUpdates()
    {
        Console.WriteLine("Checking for Tsukikage updates...");

        try
        {
            using HttpRequestMessage gitHubApiRequest = new(HttpMethod.Get, s_gitHubApiUrlForLatestTsukikageRelease);
            gitHubApiRequest.Headers.Add("User-Agent", "Tsukikage");

            using HttpResponseMessage gitHubApiResponse = await s_client.SendAsync(gitHubApiRequest, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            if (gitHubApiResponse.IsSuccessStatusCode)
            {
                JsonDocument jsonDocument;
                Stream githubApiResultStream = await gitHubApiResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await using (githubApiResultStream.ConfigureAwait(false))
                {
                    jsonDocument = await JsonDocument.ParseAsync(githubApiResultStream).ConfigureAwait(false);
                }

                using (jsonDocument)
                {
                    JsonElement rootElement = jsonDocument.RootElement;
                    string? tagName = rootElement.GetProperty("tag_name").GetString();
                    Debug.Assert(tagName is not null);

                    Version latestTsukikageVersion = new(tagName);
                    if (latestTsukikageVersion > AppInfo.TsukikageVersion)
                    {
                        string architecture = RuntimeInformation.ProcessArchitecture is Architecture.Arm64
                            ? "arm64"
                            : AppInfo.Is64BitProcess
                                ? "x64"
                                : "x86";

                        JsonElement assets = jsonDocument.RootElement.GetProperty("assets");
                        foreach (JsonElement asset in assets.EnumerateArray())
                        {
                            string? latestReleaseUrl = asset.GetProperty("browser_download_url").GetString();
                            Debug.Assert(latestReleaseUrl is not null);

                            if (latestReleaseUrl.AsSpan().Contains(architecture, StringComparison.Ordinal))
                            {
                                Console.WriteLine($"Tsukikage v{latestTsukikageVersion} is available.");
                                await UpdateTsukikage(new Uri(latestReleaseUrl)).ConfigureAwait(false);
                                break;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Tsukikage is up-to-date!");
                    }
                }
            }
            else
            {
                await Console.Error.WriteLineAsync($"Couldn't check for Tsukikage updates. GitHub API problem. {gitHubApiResponse.StatusCode} {gitHubApiResponse.ReasonPhrase}").ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Couldn't check for Tsukikage updates.\n{ex.Message}").ConfigureAwait(false);
        }
    }

    private static async Task UpdateTsukikage(Uri latestReleaseUrl)
    {
        Console.WriteLine("Updating...");
        using HttpResponseMessage downloadResponse = await s_client.GetAsync(latestReleaseUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

        if (downloadResponse.IsSuccessStatusCode)
        {
            string tmpDirectory = Path.Join(AppInfo.ApplicationPath, "tmp");

            if (Directory.Exists(tmpDirectory))
            {
                Directory.Delete(tmpDirectory, true);
            }

            _ = Directory.CreateDirectory(tmpDirectory);

            Stream downloadResponseStream = await downloadResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
            await using (downloadResponseStream.ConfigureAwait(false))
            {
                ZipArchive archive = new(downloadResponseStream);
                await using (archive.ConfigureAwait(false))
                {
                    await archive.ExtractToDirectoryAsync(tmpDirectory).ConfigureAwait(false);
                }
            }

            await Program.Cleanup().ConfigureAwait(false);

            using Process? process = Process.Start(new ProcessStartInfo
            {
                WorkingDirectory = AppInfo.ApplicationPath,
                FileName = "update-helper.cmd",
                Arguments = Environment.ProcessId.ToString(CultureInfo.InvariantCulture),
                UseShellExecute = true
            });

            WinApi.PostQuitMessage();
        }
        else
        {
            await Console.Error.WriteLineAsync($"Couldn't update Tsukikage. {downloadResponse.StatusCode} {downloadResponse.ReasonPhrase}").ConfigureAwait(false);
        }
    }

}
