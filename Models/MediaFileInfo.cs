namespace FileNexus.Models;

/// <summary>
/// 文件元数据模型，封装文件系统信息与格式化属性
/// </summary>
public class MediaFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileType { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public DateTime CreationTime { get; set; }
    public DateTime LastModified { get; set; }

    /// <summary>
    /// 格式化文件大小为可读字符串 (B/KB/MB/GB)
    /// </summary>
    public string FileSizeFormatted
    {
        get
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = FileSize;
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
