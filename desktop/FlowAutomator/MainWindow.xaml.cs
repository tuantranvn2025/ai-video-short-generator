using Microsoft.Web.WebView2.Core;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Windows;
using System;
using System.Threading.Tasks;

namespace FlowAutomator;

public partial class MainWindow : Window
{
    private readonly string cookieFilePath;

    public MainWindow()
    {
        InitializeComponent();

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "FlowAutomator");
        Directory.CreateDirectory(dir);
        cookieFilePath = Path.Combine(dir, "cookies.json");

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await webView.EnsureCoreWebView2Async();
            TxtStatus.Text = "WebView ready. Navigate to Flow and sign in.";
        }
        catch (Exception ex)
        {
            TxtStatus.Text = "WebView initialization failed: " + ex.Message;
        }
    }

    private void BtnOpenLogin_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            webView.CoreWebView2.Navigate("https://labs.google/fx/fr/tools/flow/");
            TxtStatus.Text = "Opened Flow in embedded WebView. Please sign in if needed.";
        }
        catch (Exception ex)
        {
            TxtStatus.Text = "Navigation failed: " + ex.Message;
        }
    }

    private async void BtnCaptureCookies_Click(object sender, RoutedEventArgs e)
    {
        if (webView.CoreWebView2 == null)
        {
            TxtStatus.Text = "WebView not initialized.";
            return;
        }

        try
        {
            var cookies = await webView.CoreWebView2.CookieManager.GetCookiesAsync("https://labs.google");
            var list = new System.Collections.Generic.List<object>();
            foreach (var c in cookies)
            {
                list.Add(new
                {
                    name = c.Name,
                    value = c.Value,
                    domain = c.Domain,
                    path = c.Path,
                    expires = c.Expires, // may be null
                    httpOnly = c.IsHttpOnly,
                    secure = c.IsSecure
                });
            }

            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            TxtCookies.Text = json;
            TxtStatus.Text = $"Captured {list.Count} cookies.";
        }
        catch (Exception ex)
        {
            TxtStatus.Text = "Failed to capture cookies: " + ex.Message;
        }
    }

    private void BtnSaveCookies_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var text = TxtCookies.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                TxtStatus.Text = "No cookies to save. Capture cookies first.";
                return;
            }

            File.WriteAllText(cookieFilePath, text);
            TxtStatus.Text = "Cookies saved to: " + cookieFilePath;
        }
        catch (Exception ex)
        {
            TxtStatus.Text = "Failed to save cookies: " + ex.Message;
        }
    }

    private async void BtnStartAutomation_Click(object sender, RoutedEventArgs e)
    {
        TxtStatus.Text = "Starting Playwright automation (this may open a browser)...";
        try
        {
            var pw = new PlaywrightService();
            await pw.StartAutomationWithCookiesAsync(cookieFilePath, UpdateStatus);
        }
        catch (Exception ex)
        {
            TxtStatus.Text = "Automation failed: " + ex.Message;
        }
    }

    private async void BtnDownloadUrl_Click(object sender, RoutedEventArgs e)
    {
        var url = TxtVideoUrl.Text?.Trim();
        if (string.IsNullOrEmpty(url))
        {
            TxtStatus.Text = "Please paste a video URL first.";
            return;
        }

        try
        {
            TxtStatus.Text = "Downloading video from URL...";
            var client = new System.Net.Http.HttpClient();
            using var resp = await client.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var bytes = await resp.Content.ReadAsByteArrayAsync();

            var outPath = Path.Combine(Path.GetDirectoryName(cookieFilePath)!, "downloaded_video.mp4");
            await File.WriteAllBytesAsync(outPath, bytes);
            TxtStatus.Text = "Video downloaded: " + outPath;
            // set source
            mediaPlayer.Stop();
            mediaPlayer.Source = new Uri(outPath);
            TxtVideoUrl.Text = outPath;
        }
        catch (Exception ex)
        {
            TxtStatus.Text = "Download failed: " + ex.Message;
        }
    }

    private void BtnPlayDownloaded_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = TxtVideoUrl.Text?.Trim();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                TxtStatus.Text = "No downloaded video file found. Please download first or paste a local file path.";
                return;
            }
            mediaPlayer.Stop();
            mediaPlayer.Source = new Uri(path);
            mediaPlayer.Play();
            TxtStatus.Text = "Playing: " + path;
        }
        catch (Exception ex)
        {
            TxtStatus.Text = "Failed to play video: " + ex.Message;
        }
    }

    private void UpdateStatus(string text)
    {
        Dispatcher.Invoke(() => TxtStatus.Text = text);
    }

    private void BtnClearSession_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (File.Exists(cookieFilePath)) File.Delete(cookieFilePath);
            TxtCookies.Text = string.Empty;
            TxtStatus.Text = "Session cleared.";
        }
        catch (Exception ex)
        {
            TxtStatus.Text = "Failed to clear session: " + ex.Message;
        }
    }
}
