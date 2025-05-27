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

                // �Ƴ��Ѵ��ڵ� FFmpeg ·��
                paths.RemoveAll(p => p.Contains("ffmpeg", StringComparison.OrdinalIgnoreCase));

                // �����·��
                paths.Add(ffmpegPath);
                var newPath = string.Join(Path.PathSeparator, paths);

                Environment.SetEnvironmentVariable("PATH", newPath, scope);
                return (true, "FFmpeg �ѳɹ���ӵ� PATH");
            }
            catch (Exception ex)
            {
                return (false, $"��ӵ� PATH ʧ��: {ex.Message}");
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
            progress?.Report(new DownloadProgress(0, 0, 0, "��ʼ����..."));

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
                        bytesReceived, totalBytes, percentage, "��������..."));
                }
            }

            progress?.Report(new DownloadProgress(bytesReceived, bytesReceived, 100, "���ڽ�ѹ..."));

            if (Directory.Exists(_ffmpegDir))
                Directory.Delete(_ffmpegDir, true);

            ZipFile.ExtractToDirectory(tempZip, _ffmpegDir);

            var binPath = Directory.GetDirectories(_ffmpegDir, "bin", SearchOption.AllDirectories).FirstOrDefault();
            if (string.IsNullOrEmpty(binPath))
                return (false, string.Empty, "��ѹ��δ�ҵ� bin Ŀ¼");

            if (registerToPath)
            {
                var result = await RegisterToPathAsync(binPath);
                if (!result.success)
                    return (false, binPath, result.message);
            }

            File.Delete(tempZip);
            return (true, binPath, "FFmpeg ���ز���ѹ���");
        }
        catch (OperationCanceledException)
        {
            return (false, string.Empty, "������ȡ��");
        }
        catch (Exception ex)
        {
            return (false, string.Empty, $"����ʧ��: {ex.Message}");
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
            // 1. ��黷������
            var envPaths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator);
            if (envPaths != null)
            {
                foreach (var path in envPaths)
                {
                    if (File.Exists(Path.Combine(path, "ffmpeg.exe")))
                        return path;
                }
            }

            // 2. ���Ĭ�ϰ�װ·��
            var commonPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "ffmpeg", "bin"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ffmpeg", "bin"),
                _ffmpegDir  // ���Ӧ�ó���Ŀ¼
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