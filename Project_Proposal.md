# 项目提案文档 — Project Proposal

## 项目名称

**FileNexus** — 多媒体文件管理与分析系统

## 项目概述

FileNexus 是一款基于 C# WinForms 的 Windows 桌面应用程序，旨在为用户提供一体化的文件管理解决方案。系统集成了文件浏览、全文搜索、数据统计分析、跨目录增量同步、媒体预览和元数据持久化存储六大核心功能。

## 选题理由

1. **实用性**: 文件管理是日常办公和学习的基础需求，Windows 自带的资源管理器功能有限，缺乏统计分析、批量同步等高级能力。
2. **技术覆盖**: 本项目的功能模块全面覆盖了本学期课程的核心技术点 —— C# OOP、WinForms 控件体系、文件 I/O、多线程/异步编程、数据库操作、GDI+ 图形绘制。
3. **可扩展性**: 分层架构（Models/Services/Controls）设计便于后续扩展，如添加网络同步、云存储集成等功能。

## 系统架构设计

采用三层架构：

```
┌─────────────────────────────────────┐
│           表示层 (UI)                │
│  MainForm + TabControl (6标签页)     │
│  Custom Controls (ChartControl)      │
├─────────────────────────────────────┤
│          业务逻辑层 (Services)        │
│  FileScannerService                  │
│  FileSyncService                     │
│  DatabaseService                     │
├─────────────────────────────────────┤
│           数据层 (Models)             │
│  MediaFileInfo, SyncJob              │
│  SQLite Database (FileNexus.db)      │
└─────────────────────────────────────┘
```

### 模块划分

| 模块 | 描述 | 关键技术 |
|------|------|----------|
| 文件浏览 | TreeView + ListView 双栏布局，懒加载目录树 | WinForms控件, Directory.EnumerateFiles |
| 文件搜索 | SQLite全文检索，参数化查询防注入 | ADO.NET, SQLite |
| 统计图表 | 饼图和柱状图可视化文件分布 | GDI+, Graphics.FillPie, LinearGradientBrush |
| 文件同步 | 异步增量同步，进度报告，取消支持 | async/await, Task.Run, IProgress\<T\>, CancellationToken |
| 媒体预览 | 图片加载与缩放显示 | PictureBox, Image |
| 数据库管理 | 文件元数据索引、查询、统计 | SQLite, 事务, INSERT OR REPLACE |

## 技术选型说明

| 技术 | 选择理由 |
|------|----------|
| .NET 9.0 | 最新的.NET版本，性能优化显著 |
| WinForms | 课程要求框架，成熟稳定 |
| SQLite | 嵌入式数据库，零配置，适合桌面应用 |
| System.Data.SQLite | ADO.NET提供者，兼容性好 |
| GDI+ | Windows原生2D图形引擎，无需额外依赖 |

## 开发方法

- **RDD (README驱动开发)**: 以本提案文档明确项目范围，先写文档再写代码
- **TDD (测试驱动开发)**: 对核心服务编写单元测试，RED→GREEN→REFACTOR 循环
- **AI辅助编程**: 使用 Claude Code 进行架构讨论、测试生成和代码审查

## 项目克隆地址

https://github.com/YBW-gmy/FileNexus.git
