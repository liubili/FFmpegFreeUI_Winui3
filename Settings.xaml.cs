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
using FFmpegWinUI.FFmpegService;  // ��� FFmpegService �����ռ�����

namespace FFmpegWinUI;

public sealed partial class Settings : Microsoft.UI.Xaml.Controls.Page  // ���� Page ������
{
    private readonly IFfmpegService _ffmpegService = null!; // ʹ�� null! ���
    private CancellationTokenSource _downloadCts = null!;   // ʹ�� null! ���
    private const string FfmpegPathKey = "FfmpegBinPath";

    public Settings()
    {
        try
        {
            InitializeComponent();
            
            // ʹ��App�еķ���ʵ�������û���򴴽���ʵ��
            _ffmpegService = App.FfmpegService ?? new FfmpegService(AppContext.BaseDirectory);
            _downloadCts = new CancellationTokenSource();
            
            // �첽��ʼ��
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
            // 1. �ȼ�鱣���·��
            LoadSavedPath();

            // 2. ���û�б����·���������Զ�����
            if (string.IsNullOrEmpty(FfmpegPathBox.Text))
            {
                var binPath = await _ffmpegService.FindFfmpegBinPathAsync();
                if (!string.IsNullOrEmpty(binPath))
                {
                    FfmpegPathBox.Text = binPath;
                    await CheckFfmpegPathAndShowInfoAsync(binPath);
                }
            }

            // 3. ��黷������
            if (await _ffmpegService.CheckFfmpegInPathAsync())
            {
                StatusInfoBar.Message = "FFmpeg ����ϵͳ PATH ��";
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
            ShowInfo($"FFmpeg ��Ч���汾: {version}", false);
            OpenFfmpegFolderBtn.Visibility = Visibility.Visible;
            
            // ������Ч·��
            ApplicationData.Current.LocalSettings.Values[FfmpegPathKey] = path;

            // ���� InfoBar
            StatusInfoBar.Message = $"���ҵ� FFmpeg: {version}";
            StatusInfoBar.Severity = InfoBarSeverity.Success;
            StatusInfoBar.IsOpen = true;
        }
        else
        {
            ShowInfo("��Ч�� FFmpeg ·��", true);
            OpenFfmpegFolderBtn.Visibility = Visibility.Collapsed;
            
            // ���� InfoBar
            StatusInfoBar.Message = "δ�ҵ���Ч�� FFmpeg";
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
            var progress = new Progress<FFmpegService.DownloadProgress>(p =>  // ���� DownloadProgress �������ռ�
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
                
                // ����·��
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
            ShowInfo("Ŀ¼������: " + binPath, true);
        }
    }

    private async void OnWingetInstall(object sender, RoutedEventArgs e)
    {
        WingetInstallBtn.IsEnabled = false;
        WingetStatusBorder.Visibility = Visibility.Visible;
        WingetProgressRing.IsActive = true;
        
        try
        {
            // ����Ƿ�װ�� winget
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
                ShowInfo("δ�ҵ� winget����ȷ���Ѱ�װ Windows Package Manager", true);
                return;
            }

            // ʹ�� winget ��װ FFmpeg
            WingetStatusText.Text = "���ڰ�װ FFmpeg...";
            
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
            
            // ��ȡ���
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
                ShowInfo("FFmpeg ��װ�ɹ�", false);
                // ˢ��·�����
                await InitializeAsync();
            }
            else
            {
                ShowInfo("FFmpeg ��װʧ��", true);
            }
        }
        catch (Exception ex)
        {
            ShowInfo($"��װ����: {ex.Message}", true);
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
        // ��ȡ�����أ������ں�̨��������
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
            StatusInfoBar.Title = "����";
        }
        else
        {
            ErrorInfoBadge.Visibility = Visibility.Collapsed;
            StatusInfoBar.Title = "FFmpeg ״̬";
        }

        // ����������������� InfoBadge
        var mainWindow = (FFmpegWinUI.MainWindow)App.MainWindow;
        mainWindow.SetSettingsInfoBadgeVisible(isError);
    }
}
