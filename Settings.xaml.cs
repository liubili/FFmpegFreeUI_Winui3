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
using System.Threading;
using FFmpegWinUI.FFmpegService;  // 添加 FFmpegService 命名空间引用

namespace FFmpegWinUI;

public sealed partial class Settings : Microsoft.UI.Xaml.Controls.Page  // 修正 Page 的引用
{
    private readonly IFfmpegService _ffmpegService = null!; // 使用 null! 标记
    private CancellationTokenSource _downloadCts = null!;   // 使用 null! 标记
    private const string FfmpegPathKey = "FfmpegBinPath";

    public Settings()
    {
        try
        {
            InitializeComponent();
            
            // 使用App中的服务实例，如果没有则创建新实例
            _ffmpegService = App.FfmpegService ?? new FfmpegService(AppContext.BaseDirectory);
            _downloadCts = new CancellationTokenSource();
            
            // 异步初始化
            DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    await InitializeAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Settings initialization error: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Settings constructor error: {ex.Message}");
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            // 1. 先检查保存的路径
            LoadSavedPath();

            // 2. 如果没有保存的路径，尝试自动查找
            if (string.IsNullOrEmpty(FfmpegPathBox.Text))
            {
                var binPath = await _ffmpegService.FindFfmpegBinPathAsync();
                if (!string.IsNullOrEmpty(binPath))
                {
                    FfmpegPathBox.Text = binPath;
                    await CheckFfmpegPathAndShowInfoAsync(binPath);
                }
            }

            // 3. 检查环境变量
            if (await _ffmpegService.CheckFfmpegInPathAsync())
            {
                StatusInfoBar.Message = "FFmpeg 已在系统 PATH 中";
                StatusInfoBar.Severity = InfoBarSeverity.Success;
                StatusInfoBar.IsOpen = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Initialize async error: {ex.Message}");
        }
    }

    private void LoadSavedPath()
    {
        try
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (settings.Values.TryGetValue(FfmpegPathKey, out var pathObj) && 
                pathObj is string path &&
                !string.IsNullOrWhiteSpace(path))
            {
                FfmpegPathBox.Text = path;
                _ = CheckFfmpegPathAndShowInfoAsync(path);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Load saved path error: {ex.Message}");
        }
    }

    private IntPtr GetWindowHandle()
    {
        return WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
    }

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
            await CheckFfmpegPathAndShowInfoAsync(folder.Path);
        }
    }

    private async Task CheckFfmpegPathAndShowInfoAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        if (await _ffmpegService.CheckFfmpegExistsAsync(path))
        {
            var version = await _ffmpegService.GetFfmpegVersionAsync(path);
            ShowInfo($"FFmpeg 有效，版本: {version}", false);
            OpenFfmpegFolderBtn.Visibility = Visibility.Visible;
            
            // 保存有效路径
            ApplicationData.Current.LocalSettings.Values[FfmpegPathKey] = path;

            // 更新 InfoBar
            StatusInfoBar.Message = $"已找到 FFmpeg: {version}";
            StatusInfoBar.Severity = InfoBarSeverity.Success;
            StatusInfoBar.IsOpen = true;
        }
        else
        {
            ShowInfo("无效的 FFmpeg 路径", true);
            OpenFfmpegFolderBtn.Visibility = Visibility.Collapsed;
            
            // 更新 InfoBar
            StatusInfoBar.Message = "未找到有效的 FFmpeg";
            StatusInfoBar.Severity = InfoBarSeverity.Warning;
            StatusInfoBar.IsOpen = true;
        }
    }

    private async void OnDownloadFfmpeg(object sender, RoutedEventArgs e)
    {
        DownloadFfmpegBtn.IsEnabled = false;
        ProgressGrid.Visibility = Visibility.Visible;
        ErrorInfoBadge.Visibility = Visibility.Collapsed;
        OpenFfmpegFolderBtn.Visibility = Visibility.Collapsed;

        try
        {
            var progress = new Progress<FFmpegService.DownloadProgress>(p =>  // 修正 DownloadProgress 的命名空间
            {
                DownloadProgressBar.Value = p.Percentage;
                ShowInfo(p.Status, false);
            });

            var result = await _ffmpegService.DownloadAndExtractAsync(
                RegisterToPathCheckBox.IsChecked ?? false,
                progress,
                _downloadCts.Token);

            if (result.success)
            {
                FfmpegPathBox.Text = result.path;
                OpenFfmpegFolderBtn.Visibility = Visibility.Visible;
                ShowInfo(result.message, false);
                
                // 保存路径
                ApplicationData.Current.LocalSettings.Values[FfmpegPathKey] = result.path;
            }
            else
            {
                ShowInfo(result.message, true);
            }
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

    private async void OnWingetInstall(object sender, RoutedEventArgs e)
    {
        WingetInstallBtn.IsEnabled = false;
        WingetStatusBorder.Visibility = Visibility.Visible;
        WingetProgressRing.IsActive = true;
        
        try
        {
            // 检查是否安装了 winget
            var wingetCheck = new ProcessStartInfo
            {
                FileName = "where",
                Arguments = "winget",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var checkProcess = Process.Start(wingetCheck);
            await checkProcess!.WaitForExitAsync();

            if (checkProcess.ExitCode != 0)
            {
                ShowInfo("未找到 winget，请确保已安装 Windows Package Manager", true);
                return;
            }

            // 使用 winget 安装 FFmpeg
            WingetStatusText.Text = "正在安装 FFmpeg...";
            
            var psi = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = "install FFmpeg",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            
            // 读取输出
            while (!process!.StandardOutput.EndOfStream)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                if (!string.IsNullOrEmpty(line))
                {
                    WingetStatusText.Text = line;
                }
            }

            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                ShowInfo("FFmpeg 安装成功", false);
                // 刷新路径检查
                await InitializeAsync();
            }
            else
            {
                ShowInfo("FFmpeg 安装失败", true);
            }
        }
        catch (Exception ex)
        {
            ShowInfo($"安装出错: {ex.Message}", true);
        }
        finally
        {
            WingetInstallBtn.IsEnabled = true;
            WingetProgressRing.IsActive = false;
            WingetStatusBorder.Visibility = Visibility.Collapsed;
        }
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
        // 不取消下载，让它在后台继续进行
    }

    private void ShowInfo(string message, bool isError)
    {
        InfoTextBlock.Text = message;
        StatusInfoBar.Message = message;
        StatusInfoBar.IsOpen = true;
        StatusInfoBar.Severity = isError ? InfoBarSeverity.Error : InfoBarSeverity.Success;
        
        if (isError)
        {
            ErrorInfoBadge.Visibility = Visibility.Visible;
            StatusInfoBar.Title = "错误";
        }
        else
        {
            ErrorInfoBadge.Visibility = Visibility.Collapsed;
            StatusInfoBar.Title = "FFmpeg 状态";
        }

        // 控制主窗口设置项的 InfoBadge
        var mainWindow = (FFmpegWinUI.MainWindow)App.MainWindow;
        mainWindow.SetSettingsInfoBadgeVisible(isError);
    }
}
