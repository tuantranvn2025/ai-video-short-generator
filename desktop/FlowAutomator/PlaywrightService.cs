using Microsoft.Playwright;
using System.Text.Json;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FlowAutomator;

public class PlaywrightService
{
    public async Task StartAutomationWithCookiesAsync(string cookieFilePath, Action<string> statusCallback)
    {
        if (!File.Exists(cookieFilePath))
        {
            statusCallback?.Invoke("Cookie file not found. Capture cookies from WebView2 first.");
            return;
        }

        var json = await File.ReadAllTextAsync(cookieFilePath);
        var cookies = JsonSerializer.Deserialize<List<CookieData>>(json) ?? new List<CookieData>();

        statusCallback?.Invoke($"Loaded {cookies.Count} cookies. Launching Playwright...");

        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
        var context = await browser.NewContextAsync();

        var pwCookies = new List<CookieParam>();
        foreach (var c in cookies)
        {
            // Playwright expects the domain without leading dot for some cases
            var domain = c.domain ?? "";
            pwCookies.Add(new CookieParam
            {
                Name = c.name,
                Value = c.value,
                Domain = domain,
                Path = c.path ?? "/",
                HttpOnly = c.httpOnly,
                Secure = c.secure,
                Expires = c.expires ?? null
            });
        }

        if (pwCookies.Count > 0)
        {
            await context.AddCookiesAsync(pwCookies.ToArray());
            statusCallback?.Invoke("Cookies injected into Playwright context.");
        }

        var page = await context.NewPageAsync();
        statusCallback?.Invoke("Navigating to Flow page...");
        await page.GotoAsync("https://labs.google/fx/fr/tools/flow/");

        // Attach response handler to catch video responses (e.g., .mp4 or video/* content-type)
        page.Response += async (sender, response) =>
        {
            try
            {
                var url = response.Url;
                var headers = response.Headers;
                string contentType = headers != null && headers.TryGetValue("content-type", out var ct) ? ct : string.Empty;

                if (!string.IsNullOrEmpty(contentType) && contentType.StartsWith("video"))
                {
                    statusCallback?.Invoke($"Detected video response: {url}");
                    var data = await response.BodyAsync();
                    var outPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlowAutomator", "captured_video.mp4");
                    Directory.CreateDirectory(Path.GetDirectoryName(outPath) ?? Path.GetTempPath());
                    await File.WriteAllBytesAsync(outPath, data);
                    statusCallback?.Invoke($"Video saved to: {outPath}");
                }
                else if (url.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                {
                    statusCallback?.Invoke($"Detected mp4 URL: {url}");
                    try
                    {
                        var data = await response.BodyAsync();
                        var outPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlowAutomator", "captured_video.mp4");
                        Directory.CreateDirectory(Path.GetDirectoryName(outPath) ?? Path.GetTempPath());
                        await File.WriteAllBytesAsync(outPath, data);
                        statusCallback?.Invoke($"Video saved to: {outPath}");
                    }
                    catch { /* ignore read errors */ }
                }
            }
            catch { /* swallow handler errors to avoid breaking Playwright event loop */ }
        };

        statusCallback?.Invoke("Page loaded. Monitoring network for video responses. Interact or run Flow generation in the opened browser.");

        // Keep the browser open for user observation / further automation
    }

    private class CookieData
    {
        public string? name { get; set; }
        public string? value { get; set; }
        public string? domain { get; set; }
        public string? path { get; set; }
        public double? expires { get; set; }
        public bool httpOnly { get; set; }
        public bool secure { get; set; }
    }
}
