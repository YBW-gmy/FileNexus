# FileNexus - 多媒体文件管理与分析系统

基于 C# WinForms 的 Windows 桌面应用程序，集成文件浏览、搜索、统计图表、文件同步、媒体预览和数据库管理六大功能模块。

## 技术栈

- **语言**: C# (.NET 9.0)
- **UI框架**: Windows Forms
- **数据库**: SQLite (System.Data.SQLite)
- **图形**: GDI+ 自定义图表控件
- **异步编程**: Task Parallel Library (TPL) + async/await + IProgress\<T\> + CancellationToken

## 功能模块

| 模块 | 功能 |
|------|------|
| 📁 文件浏览 | TreeView懒加载目录树 + ListView文件详情，双击打开文件 |
| 🔍 文件搜索 | 基于SQLite全文搜索，支持关键词+扩展名过滤 |
| 📊 统计图表 | GDI+自定义饼图和柱状图，渐变色+抗锯齿渲染 |
| 🔄 文件同步 | 异步增量同步，LastWriteTime比较，进度报告，协式取消 |
| 🖼️ 媒体预览 | 图片预览（支持JPG/PNG/GIF/BMP/WEBP） |
| 🗄️ 数据库管理 | 文件元数据索引，参数化查询防注入，事务管理 |

## 项目结构

```
FileNexus/
├── Models/
│   ├── MediaFileInfo.cs      # 文件元数据模型
│   └── SyncJob.cs            # 同步作业结果模型
├── Services/
│   ├── DatabaseService.cs    # SQLite数据库服务
│   ├── FileScannerService.cs # 异步文件扫描服务
│   └── FileSyncService.cs    # 文件同步服务
├── Controls/
│   └── ChartControl.cs       # 自定义GDI+图表控件
├── MainForm.cs               # 主窗体（六标签页）
├── MainForm.Designer.cs
└── Program.cs                # 入口+全局异常处理
```

## 运行

```bash
dotnet run
```

截图模式：
```bash
dotnet run -- --capture   # 自动截取所有标签页并保存到screenshots/
```

## 开发模式

本项目结合了 **RDD（README驱动开发）** 和 **TDD（测试驱动开发）**：

- RDD：先编写本README定义项目愿景和功能边界
- TDD：为关键服务（FileSyncService、DatabaseService）编写单元测试
- AI辅助：使用 Claude Code 进行架构设计讨论、测试生成和代码审查
