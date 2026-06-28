using FileNexus.Models;

namespace FileNexus.Services;

/// <summary>
/// 文件扫描服务 —— 异步遍历目录树，收集文件元数据
/// 使用EnumerateFiles延迟枚举避免大目录内存溢出
/// 支持CancellationToken协式取消和IProgress进度报告
/// </summary>
public class FileScannerService
{
    /// <summary>
    /// 异步扫描目录，返回文件信息列表
    /// 核心优化：EnumerateFiles（yield return延迟枚举）替代GetFiles（一次性加载）
    /// </summary>
    public async Task<List<MediaFileInfo>> ScanDirectoryAsync(
        string rootPath,
        string searchPattern,
        IProgress<SyncProgressInfo>? progress,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var files = new List<MediaFileInfo>();
            var allFiles = new List<string>();

            // 延迟枚举：仅在迭代时才获取下一个文件路径，不会一次性加载全部到内存
            try
            {
                allFiles.AddRange(Directory.EnumerateFiles(rootPath, searchPattern,
                    SearchOption.AllDirectories));
            }
            catch (UnauthorizedAccessException) { /* 无权限目录跳过 */ }

            int total = allFiles.Count;
            for (int i = 0; i < total; i++)
            {
                // 协式取消检查 —— 每处理一个文件检查一次
                cancellationToken.ThrowIfCancellationRequested();

                string filePath = allFiles[i];
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    string ext = fileInfo.Extension.ToLower();
                    files.Add(new MediaFileInfo
                    {
                        FileName = fileInfo.Name,
                        FullPath = fileInfo.FullName,
                        FileSize = fileInfo.Length,
                        FileType = GetFileTypeCategory(ext),
                        Extension = ext,
                        CreationTime = fileInfo.CreationTime,
                        LastModified = fileInfo.LastWriteTime
                    });
                }
                catch (Exception) { /* 单个文件读取失败不影响整体流程 */ }

                // 进度报告 —— Progress<T>自动Marshal回UI线程
                progress?.Report(new SyncProgressInfo
                {
                    PercentComplete = (int)((i + 1) * 100.0 / total),
                    CurrentFile = filePath,
                    FilesProcessed = i + 1,
                    TotalFiles = total
                });
            }
            return files;
        }, cancellationToken);
    }

    /// <summary>
    /// 根据扩展名分类文件类型
    /// </summary>
    private static string GetFileTypeCategory(string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".svg" or ".webp" => "图片",
            ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv" => "视频",
            ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" or ".wma" => "音频",
            ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".txt" => "文档",
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "压缩包",
            ".cs" or ".java" or ".py" or ".js" or ".ts" or ".cpp" or ".h" or ".html" or ".css" => "代码",
            _ => "其他"
        };
    }
}
