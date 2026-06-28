using FileNexus.Models;

namespace FileNexus.Services;

/// <summary>
/// 文件同步服务 —— 比较源目录与目标目录，执行增量同步
/// 使用LastWriteTime时间戳比较实现增量检测
/// 支持IProgress进度报告和CancellationToken取消
/// </summary>
public class FileSyncService
{
    /// <summary>
    /// 异步同步目录 —— 核心同步判断逻辑：
    /// 1. 目标不存在 → 复制新文件
    /// 2. 目标存在但源更新 → 覆盖复制
    /// 3. 目标存在且时间一致 → 跳过
    /// </summary>
    public async Task<SyncJob> SyncDirectoriesAsync(
        string sourcePath,
        string destinationPath,
        IProgress<SyncProgressInfo>? progress,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var syncJob = new SyncJob();

            // 确保目标目录存在
            if (!Directory.Exists(destinationPath))
                Directory.CreateDirectory(destinationPath);

            // 获取源目录所有文件（延迟枚举优化大目录性能）
            var sourceFiles = Directory.EnumerateFiles(sourcePath, "*.*",
                SearchOption.AllDirectories).ToArray();

            syncJob.TotalFiles = sourceFiles.Length;

            for (int i = 0; i < sourceFiles.Length; i++)
            {
                // 协式取消：每处理一个文件检查一次
                cancellationToken.ThrowIfCancellationRequested();

                string sourceFile = sourceFiles[i];
                // 计算相对路径，拼接目标完整路径
                string relativePath = Path.GetRelativePath(sourcePath, sourceFile);
                string destFile = Path.Combine(destinationPath, relativePath);

                // 确保目标子目录存在
                string? destDir = Path.GetDirectoryName(destFile);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                FileInfo sourceFileInfo = new(sourceFile);
                bool shouldCopy = false;

                if (!File.Exists(destFile))
                {
                    // 情况1：目标不存在，新文件
                    shouldCopy = true;
                }
                else
                {
                    FileInfo destFileInfo = new(destFile);
                    if (sourceFileInfo.LastWriteTime > destFileInfo.LastWriteTime)
                        shouldCopy = true; // 情况2：源文件更新
                }

                if (shouldCopy)
                {
                    File.Copy(sourceFile, destFile, true);
                    syncJob.CopiedFiles++;
                    syncJob.TotalBytesCopied += sourceFileInfo.Length;
                }
                else
                    syncJob.SkippedFiles++;

                // 进度报告通过Progress&lt;T&gt;自动回UI线程
                progress?.Report(new SyncProgressInfo
                {
                    PercentComplete = (int)((i + 1) * 100.0 / sourceFiles.Length),
                    CurrentFile = sourceFile,
                    FilesProcessed = i + 1,
                    TotalFiles = sourceFiles.Length
                });

                // 每处理10个文件让步1ms，避免长时间占用CPU
                if (i % 10 == 0)
                    Thread.Sleep(1);
            }
            return syncJob;
        }, cancellationToken);
    }
}
