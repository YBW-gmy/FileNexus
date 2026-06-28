namespace FileNexus.Models;

/// <summary>
/// 同步作业结果，记录同步过程中的统计信息
/// </summary>
public class SyncJob
{
    public int TotalFiles { get; set; }
    public int CopiedFiles { get; set; }
    public int SkippedFiles { get; set; }
    public int DeletedFiles { get; set; }
    public int ErrorFiles { get; set; }
    public long TotalBytesCopied { get; set; }

    public string TotalBytesFormatted
    {
        get
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = TotalBytesCopied;
            int unitIndex = 0;
            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }
            return $"{size:F2} {units[unitIndex]}";
        }
    }
}

/// <summary>
/// 同步进度信息，用于IProgress&lt;T&gt;报告
/// </summary>
public class SyncProgressInfo
{
    public int PercentComplete { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
    public int FilesProcessed { get; set; }
    public int TotalFiles { get; set; }
}
