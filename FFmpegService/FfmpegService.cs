using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.IO.Compression;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32;
namespace FFmpegWinUI.FFmpegService;

public class FfmpegService : IFfmpegService
{
    private readonly string _ffmpegDir;
    private readonly string _downloadUrl;
    private readonly SemaphoreSlim _downloadSemaphore = new(1, 1);
    private CancellationTokenSource _downloadCts;

    public FfmpegService(string baseDir)
    {
        _ffmpegDir = Path.Combine(baseDir, "ffmpeg");
        _downloadUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
        _downloadCts = new CancellationTokenSource();
    }

    public async Task<bool> CheckFfmpegExistsAsync(string path)
    {
        return await Task.Run(() =>
        {
            try
            {
                return File.Exists(Path.Combine(path, "ffmpeg.exe"));
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<bool> CheckFfmpegInPathAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator);
                return paths?.Any(p => File.Exists(Path.Combine(p, "ffmpeg.exe"))) ?? false;
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<string> GetFfmpegVersionAsync(string ffmpegPath)
    {
        var exePath = Path.Combine(ffmpegPath, "ffmpeg.exe");
        if (!File.Exists(exePath)) return string.Empty;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "-version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            string output = await process.StandardOutput.ReadLineAsync();
            await process.WaitForExitAsync();
            return output ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public async Task<(bool success, string message)> RegisterToPathAsync(string ffmpegPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                var scope = EnvironmentVariableTarget.User;
                var path = Environment.GetEnvironmentVariable("PATH", scope) ?? "";
                var paths = path.Split(Path.PathSeparator).ToList();

                // 移除已存在的 FFmpeg 路径
                paths.RemoveAll(p => p.Contains("ffmpeg", StringComparison.OrdinalIgnoreCase));

                // 添加新路径
                paths.Add(ffmpegPath);
                var newPath = string.Join(Path.PathSeparator, paths);

                Environment.SetEnvironmentVariable("PATH", newPath, scope);
                return (true, "FFmpeg 已成功添加到 PATH");
            }
            catch (Exception ex)
            {
                return (false, $"添加到 PATH 失败: {ex.Message}");
            }
        });
    }

    public async Task<(bool success, string path, string message)> DownloadAndExtractAsync(
        bool registerToPath = false,
        IProgress<DownloadProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        await _downloadSemaphore.WaitAsync(cancellationToken);
        try
        {
            progress?.Report(new DownloadProgress(0, 0, 0, "开始下载..."));

            string tempZip = Path.Combine(Path.GetTempPath(), "ffmpeg-latest.zip");
            using var client = new HttpClient();
            
            using var response = await client.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = File.Create(tempZip);

            var buffer = new byte[81920];
            long bytesReceived = 0;
            int read;

            while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                bytesReceived += read;
                
                if (totalBytes > 0)
                {
                    var percentage = (double)bytesReceived / totalBytes * 100;
                    progress?.Report(new DownloadProgress(
                        bytesReceived, totalBytes, percentage, "正在下载..."));
                }
            }

            progress?.Report(new DownloadProgress(bytesReceived, bytesReceived, 100, "正在解压..."));

            if (Directory.Exists(_ffmpegDir))
                Directory.Delete(_ffmpegDir, true);

            ZipFile.ExtractToDirectory(tempZip, _ffmpegDir);

            var binPath = Directory.GetDirectories(_ffmpegDir, "bin", SearchOption.AllDirectories).FirstOrDefault();
            if (string.IsNullOrEmpty(binPath))
                return (false, string.Empty, "解压后未找到 bin 目录");

            if (registerToPath)
            {
                var result = await RegisterToPathAsync(binPath);
                if (!result.success)
                    return (false, binPath, result.message);
            }

            File.Delete(tempZip);
            return (true, binPath, "FFmpeg 下载并解压完成");
        }
        catch (OperationCanceledException)
        {
            return (false, string.Empty, "下载已取消");
        }
        catch (Exception ex)
        {
            return (false, string.Empty, $"下载失败: {ex.Message}");
        }
        finally
        {
            _downloadSemaphore.Release();
        }
    }

    public async Task<string?> FindFfmpegBinPathAsync()
    {
        try
        {
            // 1. 检查环境变量
            var envPaths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator);
            if (envPaths != null)
            {
                foreach (var path in envPaths)
                {
                    if (File.Exists(Path.Combine(path, "ffmpeg.exe")))
                        return path;
                }
            }

            // 2. 检查默认安装路径
            var commonPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "ffmpeg", "bin"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ffmpeg", "bin"),
                _ffmpegDir  // 检查应用程序目录
            };

            foreach (var path in commonPaths)
            {
                if (await CheckFfmpegExistsAsync(path))
                    return path;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}