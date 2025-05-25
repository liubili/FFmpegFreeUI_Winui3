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

    // ��ȡ��ǰ���ھ��
    private IntPtr GetWindowHandle()
    {
        return WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
    }

    // ѡ��FFmpeg bin·��
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
            ShowInfo("��ѡ��FFmpeg·��", false);
        }
    }

    // ��������FFmpeg
    private async void OnDownloadFfmpeg(object sender, RoutedEventArgs e)
    {
        DownloadFfmpegBtn.IsEnabled = false;
        DownloadProgressBar.Visibility = Visibility.Visible;
        DownloadProgressBar.Value = 0;
        ShowInfo("���ڻ�ȡ����FFmpeg�汾...", false);
        ErrorInfoBadge.Visibility = Visibility.Collapsed;
        OpenFfmpegFolderBtn.Visibility = Visibility.Collapsed;

        try
        {
            string ffmpegUrl = await GetLatestFfmpegUrl();
            if (string.IsNullOrEmpty(ffmpegUrl))
            {
                ShowInfo("δ�ܻ�ȡ�������ӡ�", true);
                    return;
            }

            // ���ص���ʱ�ļ�
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
            ShowInfo("������ɣ����ڽ�ѹ...", false);

            // ��ѹ������Ŀ¼�� ffmpeg �ļ���
            if (Directory.Exists(ffmpegDir))
                Directory.Delete(ffmpegDir, true);
            ZipFile.ExtractToDirectory(tempZip, ffmpegDir);

            // ���� bin Ŀ¼
            var binPath = Directory.GetDirectories(ffmpegDir, "bin", SearchOption.AllDirectories).FirstOrDefault();
            if (binPath != null)
            {
                FfmpegPathBox.Text = binPath;
                ShowInfo("FFmpeg ���ز���ѹ���", false);
                OpenFfmpegFolderBtn.Visibility = Visibility.Visible;
            }
            else
            {
                ShowInfo("��ѹʧ�ܣ�δ�ҵ� bin Ŀ¼��", true);
            }
        }
        catch (Exception ex)
        {
            ShowInfo("���ػ��ѹʧ��: " + ex.Message, true);
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
            ShowInfo("Ŀ¼������: " + binPath, true);
        }
    }

    // ��ȡ����FFmpeg�������ӣ���gyan.devΪ����ʵ�ʿɸ�����Ҫ����Դ��
    private async Task<string> GetLatestFfmpegUrl()
    {
        // ����ֱ�ӷ���gyan.dev������win64��̬������
        // ʵ�ʿ�ͨ�������API��ȡ���°汾
        await Task.CompletedTask;
        return "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
    }

    // ��ʾ��Ϣ�ʹ���
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
