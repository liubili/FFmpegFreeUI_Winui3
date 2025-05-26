using System;
using System.Threading;
using System.Threading.Tasks;

namespace FFmpegFreeUI.FFmpegService;

public interface IFfmpegService
{
    Task<bool> CheckFfmpegExistsAsync(string path);
    Task<bool> CheckFfmpegInPathAsync();
    Task<string> GetFfmpegVersionAsync(string ffmpegPath);
    Task<(bool success, string message)> RegisterToPathAsync(string ffmpegPath);
    Task<(bool success, string path, string message)> DownloadAndExtractAsync(
        bool registerToPath = false,
        IProgress<DownloadProgress> progress = null,
        CancellationToken cancellationToken = default);
    Task<string?> FindFfmpegBinPathAsync();  // 添加新方法
}

public record DownloadProgress(
    long BytesReceived,
    long TotalBytes,
    double Percentage,
    string Status);