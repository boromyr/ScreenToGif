using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ScreenToGif.Util;

namespace ScreenToGif.Windows.Other;

public partial class Downloader
{
    #region Properties

    private bool _cancel;

    public string DownloadMode { get; set; }

    public string DestinationPath { get; set; }

    #endregion

    public Downloader()
    {
        InitializeComponent();
    }


    private string GetDownloadUrl()
    {
        switch (DownloadMode)
        {
            case "gifski":
                return "https://nickemanarin.github.io/ScreenToGif-Website/downloads/Gifski.zip";
            case "ffmpeg":
            {
                return Environment.Is64BitProcess ? "https://nickemanarin.github.io/ScreenToGif-Website/downloads/FFmpeg-4.4.1-x64.zip" :
                    "https://nickemanarin.github.io/ScreenToGif-Website/downloads/FFmpeg-4.3.1-x86.zip";
            }
        }

        return null;
    }

    private async Task Download()
    {
        try
        {
            //Save to a temp folder.
            var temp = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
            var downloadUrl = GetDownloadUrl();

            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                StatusBand.Error("Download URL not provided...");
                return;
            }

            //Download.
            using (var handler = new HttpClientHandler { Proxy = WebRequest.GetSystemWebProxy() })
            using (var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan })
            using (var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                var total = response.Content.Headers.ContentLength ?? -1;
                var received = 0L;
                var buffer = new byte[81920];

                await using var source = await response.Content.ReadAsStreamAsync();
                await using var target = File.Create(temp);

                int read;
                while ((read = await source.ReadAsync(buffer)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, read));
                    received += read;

                    TotalTextBlock.Text = LocalizationHelper.GetWithFormat("S.Downloader.Size", "{0} of {1}", Humanizer.BytesToString(received), Humanizer.BytesToString(total));
                    MainProgressBar.Value = total > 0 ? received * 100d / total : 0;

                    if (_cancel)
                        Environment.Exit(35);
                }
            }

            if (_cancel)
            {
                Environment.Exit(90);
                return;
            }

            //Decompress.
            using (var zip = ZipFile.Open(temp, ZipArchiveMode.Read))
            {
                switch (DownloadMode)
                {
                    case "gifski":
                    {
                        var entry = zip.Entries.FirstOrDefault(x => x.Name.Contains("gifski.dll"));

                        if (File.Exists(DestinationPath))
                            File.Delete(DestinationPath);

                        entry?.ExtractToFile(DestinationPath, true);
                        break;
                    }
                    case "ffmpeg":
                    {
                        var entry = zip.Entries.FirstOrDefault(x => x.Name.Contains("ffmpeg.exe"));

                        if (File.Exists(DestinationPath))
                            File.Delete(DestinationPath);

                        entry?.ExtractToFile(DestinationPath, true);
                        break;
                    }
                    case "sharpdx":
                    {
                        foreach (var entry in zip.Entries)
                        {
                            if (File.Exists(Path.Combine(DestinationPath, entry.Name)))
                                File.Delete(Path.Combine(DestinationPath, entry.Name));

                            entry?.ExtractToFile(Path.Combine(DestinationPath, entry.Name), true);
                        }

                        break;
                    }
                }
            }

            File.Delete(temp);
            Environment.Exit(10);
        }
        catch (Exception e)
        {
            LogWriter.Log(e, "Impossible to download");

            StatusBand.Error(e.Message);
            RetryButton.IsEnabled = true;
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await Download();
    }
        
    private async void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        if (!RetryButton.IsEnabled || !IsLoaded)
            return;

        RetryButton.IsEnabled = false;

        await Download();

        CancelButton.IsEnabled = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (!CancelButton.IsEnabled || !IsLoaded)
            return;

        CancelButton.IsEnabled = false;
        _cancel = true;
    }
}