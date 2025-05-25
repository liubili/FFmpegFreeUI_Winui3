using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.IO.Compression;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FFmpegFreeUI;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Settings : Microsoft.UI.Xaml.Controls.Page
{
    private readonly string ffmpegDir = Path.Combine(AppContext.BaseDirectory, "ffmpeg");

    public Settings()
    {
        InitializeComponent();
        DownloadProgressBar.Visibility = Visibility.Collapsed;
        ErrorInfoBadge.Visibility = Visibility.Collapsed;
        OpenFfmpegFolderBtn.Visibility = Visibility.Collapsed;
    }

    // 获取当前窗口句柄
    private IntPtr GetWindowHandle()
    {
        return WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
    }

    // 选择FFmpeg bin路径
    private async void OnSelectFfmpegPath(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        var hwnd = GetWindowHandle();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        picker.FileTypeFilter.Add("*");

        StorageFolder folder = await picker.PickSingleFolderAsync();
        if (folder != null)
        {
            FfmpegPathBox.Text = folder.Path;
            ShowInfo("已选择FFmpeg路径", false);
        }
    }

    // 下载最新FFmpeg
    private async void OnDownloadFfmpeg(object sender, RoutedEventArgs e)
    {
        DownloadFfmpegBtn.IsEnabled = false;
        DownloadProgressBar.Visibility = Visibility.Visible;
        DownloadProgressBar.Value = 0;
        ShowInfo("正在获取最新FFmpeg版本...", false);
        ErrorInfoBadge.Visibility = Visibility.Collapsed;
        OpenFfmpegFolderBtn.Visibility = Visibility.Collapsed;

        try
        {
            string ffmpegUrl = await GetLatestFfmpegUrl();
            if (string.IsNullOrEmpty(ffmpegUrl))
            {
                ShowInfo("未能获取下载链接。", true);
                    return;
            }

            // 下载到临时文件
            string tempZip = Path.Combine(Path.GetTempPath(), "ffmpeg-latest.zip");
            using (var http = new HttpClient())
            using (var response = await http.GetAsync(ffmpegUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength ?? 0L;
                var canReport = total > 0;

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fs = File.Create(tempZip))
                {
                    var buffer = new byte[81920];
                    long read = 0;
                    int n;
                    while ((n = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fs.WriteAsync(buffer, 0, n);
                        read += n;
                        if (canReport)
                        {
                            DownloadProgressBar.Value = (double)read / total * 100;
                        }
                    }
                }
            }

            DownloadProgressBar.Value = 100;
            ShowInfo("下载完成，正在解压...", false);

            // 解压到程序目录下 ffmpeg 文件夹
            if (Directory.Exists(ffmpegDir))
                Directory.Delete(ffmpegDir, true);
            ZipFile.ExtractToDirectory(tempZip, ffmpegDir);

            // 查找 bin 目录
            var binPath = Directory.GetDirectories(ffmpegDir, "bin", SearchOption.AllDirectories).FirstOrDefault();
            if (binPath != null)
            {
                FfmpegPathBox.Text = binPath;
                ShowInfo("FFmpeg 下载并解压完成", false);
                OpenFfmpegFolderBtn.Visibility = Visibility.Visible;
            }
            else
            {
                ShowInfo("解压失败，未找到 bin 目录。", true);
            }
        }
        catch (Exception ex)
        {
            ShowInfo("下载或解压失败: " + ex.Message, true);
        }
        finally
        {
            DownloadFfmpegBtn.IsEnabled = true;
            DownloadProgressBar.Visibility = Visibility.Collapsed;
        }
    }

    private void OnOpenFfmpegFolder(object sender, RoutedEventArgs e)
    {
        var binPath = FfmpegPathBox.Text;
        if (Directory.Exists(binPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = binPath,
                UseShellExecute = true
            });
        }
        else
        {
            ShowInfo("目录不存在: " + binPath, true);
        }
    }

    // 获取最新FFmpeg下载链接（以gyan.dev为例，实际可根据需要更换源）
    private async Task<string> GetLatestFfmpegUrl()
    {
        // 这里直接返回gyan.dev的最新win64静态版链接
        // 实际可通过爬虫或API获取最新版本
        await Task.CompletedTask;
        return "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
    }

    // 显示信息和错误
    private void ShowInfo(string message, bool isError)
    {
        InfoTextBlock.Text = message;
        if (isError)
        {
            InfoTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
            ErrorInfoBadge.Visibility = Visibility.Visible;
        }
        else
        {
            InfoTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
            ErrorInfoBadge.Visibility = Visibility.Collapsed;
        }
    }
}
