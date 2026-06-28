using System.Data;
using FileNexus.Controls;
using FileNexus.Models;
using FileNexus.Services;

namespace FileNexus;

/// <summary>
/// FileNexus 主窗体 —— 多媒体文件管理与分析系统
/// 各功能模块分布在TabControl的独立标签页中：
/// 1.文件浏览  2.文件搜索  3.统计图表  4.文件同步  5.媒体预览  6.数据库管理
/// </summary>
public partial class MainForm : Form
{
    // ---- 核心服务 ----
    private FileScannerService? _scannerService;
    private FileSyncService? _syncService;
    private DatabaseService? _dbService;
    private CancellationTokenSource? _scanCts;
    private CancellationTokenSource? _syncCts;

    // ---- 控件引用 ----
    private TabControl? _tabControl;
    private StatusStrip? _statusStrip;
    private ToolStripStatusLabel? _lblStatus;
    private ToolStripProgressBar? _tsProgress;

    // ---- Tab1: 文件浏览 ----
    private SplitContainer? _splitContainer;
    private TreeView? _treeView;
    private ListView? _listView;
    private TextBox? _txtBrowserPath;

    // ---- Tab2: 文件搜索 ----
    private TextBox? _txtSearchKeyword;
    private ComboBox? _cmbSearchExt;
    private Button? _btnSearch;
    private DataGridView? _dgvSearchResults;

    // ---- Tab3: 统计图表 ----
    private ChartControl? _pieChart;
    private ChartControl? _barChart;

    // ---- Tab4: 文件同步 ----
    private TextBox? _txtSourcePath;
    private TextBox? _txtDestPath;
    private Button? _btnStartSync;
    private Button? _btnCancelSync;
    private ProgressBar? _progressSync;
    private Label? _lblSyncProgress;

    // ---- Tab5: 媒体预览 ----
    private ListBox? _lbMediaFiles;
    private PictureBox? _picPreview;
    private Panel? _mediaPanel;

    // ---- Tab6: 数据库管理 ----
    private Button? _btnIndexFiles;
    private Button? _btnShowStats;
    private DataGridView? _dgvDbFiles;
    private Label? _lblDbStats;

    private string? _currentDir;

    public MainForm()
    {
        InitializeComponent();
        InitServices();
        LoadDrives();
    }

    #region 初始化 —— 窗体和控件创建（不使用Designer，纯代码构建UI）

    private void InitializeComponent()
    {
        Text = "FileNexus - 多媒体文件管理与分析系统";
        Size = new Size(1200, 750);
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("微软雅黑", 9F);

        // 顶部路径栏
        var topPanel = new Panel { Dock = DockStyle.Top, Height = 32, Padding = new Padding(4) };
        _txtBrowserPath = new TextBox { Text = @"C:\", Width = 500, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
        var btnBrowse = new Button { Text = "浏览", Width = 60, Left = 510, Anchor = AnchorStyles.Top | AnchorStyles.Left };
        btnBrowse.Click += BtnBrowse_Click;
        topPanel.Controls.Add(_txtBrowserPath);
        topPanel.Controls.Add(btnBrowse);

        // 标签页控件
        _tabControl = new TabControl { Dock = DockStyle.Fill };

        // ---- Tab1: 文件浏览 ----
        var tab1 = new TabPage("📁 文件浏览");
        _splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill
        };
        _treeView = new TreeView { Dock = DockStyle.Fill };
        _treeView.BeforeExpand += TreeView_BeforeExpand;
        _treeView.AfterSelect += TreeView_AfterSelect;
        _splitContainer.Panel1.Controls.Add(_treeView);

        _listView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };
        _listView.Columns.Add("文件名", 260);
        _listView.Columns.Add("大小", 90);
        _listView.Columns.Add("类型", 80);
        _listView.Columns.Add("修改时间", 160);
        _listView.DoubleClick += ListView_DoubleClick;
        _splitContainer.Panel2.Controls.Add(_listView);
        tab1.Controls.Add(_splitContainer);
        _tabControl.TabPages.Add(tab1);

        // ---- Tab2: 文件搜索 ----
        var tab2 = new TabPage("🔍 文件搜索");
        var searchPanel = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(4) };
        searchPanel.Controls.Add(new Label { Text = "关键词:", Left = 4, Top = 10, Width = 55 });
        _txtSearchKeyword = new TextBox { Left = 62, Top = 7, Width = 180 };
        searchPanel.Controls.Add(_txtSearchKeyword);
        searchPanel.Controls.Add(new Label { Text = "扩展名:", Left = 250, Top = 10, Width = 55 });
        _cmbSearchExt = new ComboBox { Left = 308, Top = 7, Width = 100 };
        _cmbSearchExt.Items.AddRange(new[] { "", ".jpg", ".png", ".mp4", ".mp3", ".pdf", ".docx", ".cs", ".zip" });
        searchPanel.Controls.Add(_cmbSearchExt);
        _btnSearch = new Button { Text = "搜索", Left = 415, Top = 5, Width = 70 };
        _btnSearch.Click += BtnSearch_Click;
        searchPanel.Controls.Add(_btnSearch);
        _dgvSearchResults = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        tab2.Controls.Add(_dgvSearchResults);
        tab2.Controls.Add(searchPanel);
        _tabControl.TabPages.Add(tab2);

        // ---- Tab3: 统计图表 ----
        var tab3 = new TabPage("📊 统计图表");
        var chartSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical
        }; // SplitterDistance在布局后自动计算
        _pieChart = new ChartControl { Dock = DockStyle.Fill };
        _barChart = new ChartControl { Dock = DockStyle.Fill };
        chartSplit.Panel1.Controls.Add(_pieChart);
        chartSplit.Panel2.Controls.Add(_barChart);
        tab3.Controls.Add(chartSplit);
        _tabControl.TabPages.Add(tab3);

        // ---- Tab4: 文件同步 ----
        var tab4 = new TabPage("🔄 文件同步");
        var syncPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(12)
        };
        syncPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        syncPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        syncPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        syncPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        syncPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        syncPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        syncPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

        syncPanel.Controls.Add(new Label { Text = "源目录:" }, 0, 0);
        _txtSourcePath = new TextBox { Dock = DockStyle.Fill, Text = @"C:\Users\Public\Documents" };
        syncPanel.Controls.Add(_txtSourcePath, 1, 0);
        syncPanel.Controls.Add(new Label { Text = "目标目录:" }, 0, 1);
        _txtDestPath = new TextBox { Dock = DockStyle.Fill, Text = @"D:\FileNexus_Backup" };
        syncPanel.Controls.Add(_txtDestPath, 1, 1);

        var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };
        _btnStartSync = new Button { Text = "▶ 开始同步", Width = 100, BackColor = Color.FromArgb(65, 140, 240), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnStartSync.FlatAppearance.BorderSize = 0;
        _btnStartSync.Click += BtnStartSync_Click;
        _btnCancelSync = new Button { Text = "■ 取消", Width = 80, BackColor = Color.FromArgb(237, 101, 89), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Enabled = false };
        _btnCancelSync.FlatAppearance.BorderSize = 0;
        _btnCancelSync.Click += (s, e) => { _syncCts?.Cancel(); };
        btnPanel.Controls.Add(_btnStartSync);
        btnPanel.Controls.Add(_btnCancelSync);
        syncPanel.Controls.Add(btnPanel, 1, 2);

        _progressSync = new ProgressBar { Dock = DockStyle.Fill, Height = 25 };
        _lblSyncProgress = new Label { Text = "就绪", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        syncPanel.Controls.Add(_progressSync, 1, 3);
        syncPanel.Controls.Add(_lblSyncProgress, 1, 4);
        tab4.Controls.Add(syncPanel);
        _tabControl.TabPages.Add(tab4);

        // ---- Tab5: 媒体预览 ----
        var tab5 = new TabPage("🖼️ 媒体预览");
        var mediaSplit = new SplitContainer
        {
            Dock = DockStyle.Fill
        }; // SplitterDistance在布局后自动计算
        _lbMediaFiles = new ListBox { Dock = DockStyle.Fill };
        _lbMediaFiles.SelectedIndexChanged += LbMediaFiles_SelectedIndexChanged;
        mediaSplit.Panel1.Controls.Add(_lbMediaFiles);

        _mediaPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        _picPreview = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(245, 245, 250)
        };
        _mediaPanel.Controls.Add(_picPreview);
        mediaSplit.Panel2.Controls.Add(_mediaPanel);
        tab5.Controls.Add(mediaSplit);
        _tabControl.TabPages.Add(tab5);

        // ---- Tab6: 数据库管理 ----
        var tab6 = new TabPage("🗄️ 数据库管理");
        var dbPanel = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(4) };
        _btnIndexFiles = new Button { Text = "索引当前目录", Width = 110, Left = 4, Top = 5, BackColor = Color.FromArgb(89, 194, 121), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnIndexFiles.FlatAppearance.BorderSize = 0;
        _btnIndexFiles.Click += BtnIndexFiles_Click;
        _btnShowStats = new Button { Text = "刷新统计", Width = 90, Left = 120, Top = 5, BackColor = Color.FromArgb(65, 140, 240), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnShowStats.FlatAppearance.BorderSize = 0;
        _btnShowStats.Click += BtnShowStats_Click;
        dbPanel.Controls.Add(_btnIndexFiles);
        dbPanel.Controls.Add(_btnShowStats);
        _lblDbStats = new Label { Text = "数据库就绪", Left = 220, Top = 10, AutoSize = true };
        dbPanel.Controls.Add(_lblDbStats);

        _dgvDbFiles = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
        tab6.Controls.Add(_dgvDbFiles);
        tab6.Controls.Add(dbPanel);
        _tabControl.TabPages.Add(tab6);

        // 截图菜单
        var menuStrip = new MenuStrip();
        var helpMenu = new ToolStripMenuItem("工具");
        var captureMenu = new ToolStripMenuItem("📷 生成所有截图", null, (s, e) => CaptureFullWindow());
        helpMenu.DropDownItems.Add(captureMenu);
        menuStrip.Items.Add(helpMenu);
        Controls.Add(menuStrip);

        // 状态栏
        _statusStrip = new StatusStrip();
        _lblStatus = new ToolStripStatusLabel("就绪");
        _tsProgress = new ToolStripProgressBar { Style = ProgressBarStyle.Marquee, Visible = false, Width = 120 };
        _statusStrip.Items.Add(_lblStatus);
        _statusStrip.Items.Add(_tsProgress);

        // 组装窗体（Dock顺序：先Top->Fill->Bottom）
        Controls.Add(menuStrip);     // Top - 最顶部
        Controls.Add(topPanel);     // Top - 次顶部
        Controls.Add(_tabControl);  // Fill
        Controls.Add(_statusStrip); // Bottom

        // MinSize和SplitterDistance必须在窗体布局完成后设置，否则Width尚为默认值会越界
        Load += (s, e) =>
        {
            _splitContainer.Panel1MinSize = 180;
            _splitContainer.Panel2MinSize = 400;
            _splitContainer.SplitterDistance = 280;
        };
    }

    #endregion

    #region 服务初始化

    private void InitServices()
    {
        _scannerService = new FileScannerService();
        _syncService = new FileSyncService();
        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileNexus.db");
        _dbService = new DatabaseService(dbPath);
    }

    #endregion

    #region Tab1: 文件浏览 —— TreeView懒加载 + ListView详情

    private void LoadDrives()
    {
        _treeView?.Nodes.Clear();
        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.IsReady)
            {
                var node = new TreeNode($"{drive.Name} ({drive.VolumeLabel})")
                {
                    Tag = drive.RootDirectory.FullName,
                };
                node.Nodes.Add(new TreeNode("loading..."));
                _treeView?.Nodes.Add(node);
            }
        }
    }

    /// <summary>
    /// 懒加载策略：展开节点时才加载子目录
    /// 初始只加载根目录+一级子目录，避免递归扫描整个磁盘
    /// </summary>
    private void TreeView_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
    {
        if (e.Node?.Tag is not string path) return;
        try
        {
            e.Node.Nodes.Clear();
            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                var dirInfo = new DirectoryInfo(dir);
                var node = new TreeNode(dirInfo.Name) { Tag = dir };
                // 仅当存在子目录时添加占位节点
                try
                {
                    if (Directory.EnumerateDirectories(dir).Any())
                        node.Nodes.Add(new TreeNode("loading..."));
                }
                catch { }
                e.Node.Nodes.Add(node);
            }
        }
        catch (UnauthorizedAccessException) { /* 无权限目录静默跳过 */ }
    }

    /// <summary>
    /// 选中目录后，在ListView中展示该目录下的文件详情
    /// </summary>
    private void TreeView_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is not string path) return;
        _currentDir = path;
        _txtBrowserPath!.Text = path;
        RefreshFileList(path);
    }

    private void RefreshFileList(string path)
    {
        _listView?.Items.Clear();
        try
        {
            var dirInfo = new DirectoryInfo(path);
            foreach (var file in dirInfo.EnumerateFiles())
            {
                var item = new ListViewItem(file.Name);
                item.SubItems.Add(FormatFileSize(file.Length));
                item.SubItems.Add(file.Extension.ToLower());
                item.SubItems.Add(file.LastWriteTime.ToString("yyyy-MM-dd HH:mm"));
                item.Tag = file.FullName;
                _listView?.Items.Add(item);
            }
            _lblStatus!.Text = $"{_listView?.Items.Count ?? 0} 个文件";
        }
        catch (UnauthorizedAccessException)
        {
            _lblStatus!.Text = "无权限访问此目录";
        }
    }

    private void ListView_DoubleClick(object? sender, EventArgs e)
    {
        if (_listView?.SelectedItems.Count > 0 && _listView.SelectedItems[0].Tag is string filePath)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show($"无法打开文件: {ex.Message}"); }
        }
    }

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog();
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _txtBrowserPath!.Text = dlg.SelectedPath;
            RefreshFileList(dlg.SelectedPath);
        }
    }

    #endregion

    #region Tab2: 文件搜索 —— 数据库搜索 + DataGridView展示

    private void BtnSearch_Click(object? sender, EventArgs e)
    {
        if (_dbService == null || _dgvSearchResults == null) return;
        string keyword = _txtSearchKeyword?.Text ?? "";
        string ext = _cmbSearchExt?.Text ?? "";

        var results = _dbService.SearchFiles(keyword, ext);
        var dt = new DataTable();
        dt.Columns.Add("文件名");
        dt.Columns.Add("大小");
        dt.Columns.Add("类型");
        dt.Columns.Add("扩展名");
        dt.Columns.Add("完整路径");
        dt.Columns.Add("修改时间");
        foreach (var f in results)
        {
            dt.Rows.Add(f.FileName, f.FileSizeFormatted, f.FileType, f.Extension, f.FullPath, f.LastModified);
        }
        _dgvSearchResults.DataSource = dt;
        _lblStatus!.Text = $"找到 {results.Count} 个文件";
    }

    #endregion

    #region Tab3: 统计图表 —— GDI+自定义控件展示

    private void RefreshCharts()
    {
        if (_dbService == null || _pieChart == null || _barChart == null) return;
        var stats = _dbService.GetFileTypeStatistics();
        if (stats.Count == 0)
        {
            _lblStatus!.Text = "请先在「数据库管理」页索引文件";
            return;
        }
        _pieChart.SetData(stats, 0);
        // 柱状图展示文件大小分布（使用同一数据模拟）
        _barChart.SetData(stats, 1);
    }

    #endregion

    #region Tab4: 文件同步 —— async/await + IProgress + CancellationToken

    /// <summary>
    /// 异步文件同步 —— 完整展示C#异步编程模式：
    /// 1. async void事件处理器（符合WinForms约定）
    /// 2. await Task.Run将耗时操作移交线程池
    /// 3. IProgress&lt;T&gt;自动Marshal回UI线程
    /// 4. CancellationToken协式取消
    /// 5. try-catch-finally保证UI状态恢复
    /// </summary>
    private async void BtnStartSync_Click(object? sender, EventArgs e)
    {
        string source = _txtSourcePath?.Text ?? "";
        string dest = _txtDestPath?.Text ?? "";

        if (!Directory.Exists(source))
        {
            MessageBox.Show("请选择有效的源目录！", "FileNexus", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // 取消之前的操作
        _syncCts?.Cancel();
        _syncCts = new CancellationTokenSource();

        SetSyncingState(true);

        try
        {
            // Progress&lt;T&gt;通过SynchronizationContext自动将Report回调回UI线程
            var progress = new Progress<SyncProgressInfo>(info =>
            {
                _progressSync!.Value = info.PercentComplete;
                _lblSyncProgress!.Text = $"同步中: {info.FilesProcessed}/{info.TotalFiles} - {info.PercentComplete}%";
            });

            // await挂起UI线程，释放消息循环处理用户交互
            SyncJob result = await _syncService!.SyncDirectoriesAsync(
                source, dest, progress, _syncCts.Token);

            // await之后的代码自动回到UI线程
            _dbService?.RecordSyncHistory(source, dest, result);
            string msg = $"同步完成！\n共 {result.TotalFiles} 个文件\n" +
                         $"复制: {result.CopiedFiles} | 跳过: {result.SkippedFiles}\n" +
                         $"数据量: {result.TotalBytesFormatted}";
            MessageBox.Show(msg, "FileNexus", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _lblSyncProgress!.Text = "同步完成";
        }
        catch (OperationCanceledException)
        {
            _lblSyncProgress!.Text = "同步已取消";
            _lblStatus!.Text = "同步已取消";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"同步出错: {ex.Message}", "FileNexus", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            // 保证UI状态恢复，即使发生异常
            SetSyncingState(false);
        }
    }

    private void SetSyncingState(bool syncing)
    {
        _btnStartSync!.Enabled = !syncing;
        _btnCancelSync!.Enabled = syncing;
        _tsProgress!.Visible = syncing;
    }

    #endregion

    #region Tab5: 媒体预览 —— PictureBox图片预览

    private void LbMediaFiles_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_lbMediaFiles?.SelectedItem is not string filePath) return;
        try
        {
            string ext = Path.GetExtension(filePath).ToLower();
            if (ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp")
            {
                _picPreview!.Image = Image.FromFile(filePath);
            }
            else
            {
                _picPreview!.Image = null;
                _lblStatus!.Text = "仅支持图片预览";
            }
        }
        catch (Exception ex)
        {
            _picPreview!.Image = null;
            _lblStatus!.Text = $"无法加载图片: {ex.Message}";
        }
    }

    #endregion

    #region Tab6: 数据库管理 —— 索引文件 + 展示统计

    private async void BtnIndexFiles_Click(object? sender, EventArgs e)
    {
        if (_scannerService == null || _dbService == null) return;
        string path = _txtBrowserPath?.Text ?? _currentDir ?? @"C:\";
        _btnIndexFiles!.Enabled = false;
        _tsProgress!.Visible = true;
        _lblStatus!.Text = "正在扫描文件...";

        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<SyncProgressInfo>(info =>
            {
                _lblStatus!.Text = $"扫描中: {info.FilesProcessed}/{info.TotalFiles} ({info.PercentComplete}%)";
            });

            var files = await _scannerService.ScanDirectoryAsync(path, "*.*", progress, _scanCts.Token);
            _dbService.BatchInsertFiles(files);
            _lblDbStats!.Text = $"已索引 {files.Count} 个文件 (数据库: FileNexus.db)";
            _lblStatus!.Text = $"索引完成: {files.Count} 个文件";
            RefreshCharts();
            RefreshDbGrid();
        }
        catch (OperationCanceledException)
        {
            _lblStatus!.Text = "扫描已取消";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"索引失败: {ex.Message}", "FileNexus");
        }
        finally
        {
            _btnIndexFiles.Enabled = true;
            _tsProgress!.Visible = false;
        }
    }

    private void BtnShowStats_Click(object? sender, EventArgs e)
    {
        RefreshCharts();
        RefreshDbGrid();
    }

    private void RefreshDbGrid()
    {
        if (_dbService == null || _dgvDbFiles == null) return;
        var files = _dbService.SearchFiles("", "");
        var dt = new DataTable();
        dt.Columns.Add("文件名");
        dt.Columns.Add("大小");
        dt.Columns.Add("类型");
        dt.Columns.Add("扩展名");
        dt.Columns.Add("修改时间");
        foreach (var f in files)
            dt.Rows.Add(f.FileName, f.FileSizeFormatted, f.FileType, f.Extension, f.LastModified);
        _dgvDbFiles.DataSource = dt;
        _lblDbStats!.Text = $"数据库共 {files.Count} 条记录";
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 截图前自动索引数据：扫描C:\Windows\Media目录（音频）+ C:\Windows\Web目录（图片）
    /// 填充数据库、列表视图和媒体预览
    /// </summary>
    public async Task AutoIndexForCapture()
    {
        try
        {
            _lblStatus!.Text = "正在为截图准备数据...";
            _tsProgress!.Visible = true;

            var scanPaths = new[] {
                @"C:\Windows\Web",      // Windows壁纸（图片丰富）
                @"C:\Windows\Media",    // 系统音效
                @"C:\Windows\Fonts",    // 字体文件（类型多样）
            };

            var allFiles = new List<Models.MediaFileInfo>();
            foreach (var path in scanPaths)
            {
                if (!Directory.Exists(path)) continue;
                _scanCts?.Cancel();
                _scanCts = new CancellationTokenSource();
                try
                {
                    var files = await _scannerService!.ScanDirectoryAsync(
                        path, "*.*", null, _scanCts.Token);
                    allFiles.AddRange(files);
                }
                catch { }
            }

            if (allFiles.Count > 0)
            {
                _dbService!.BatchInsertFiles(allFiles);
                _lblDbStats!.Text = $"已索引 {allFiles.Count} 个文件";
                _lblStatus!.Text = $"索引完成: {allFiles.Count} 个文件";
            }

            // 刷新各视图
            if (allFiles.Count > 0)
            {
                _txtBrowserPath!.Text = @"C:\Windows\Web";
                _currentDir = @"C:\Windows\Web";
                RefreshFileList(@"C:\Windows\Web");

                // 切换到Tab1展示目录树
                if (Directory.Exists(@"C:\Windows\Web"))
                {
                    var node = _treeView!.Nodes.Cast<TreeNode>()
                        .FirstOrDefault(n => n.Tag?.ToString() == @"C:\");
                    if (node != null)
                    {
                        node.Expand();
                        Application.DoEvents();
                        Thread.Sleep(300);
                        // 找到Windows节点
                        var winNode = node.Nodes.Cast<TreeNode>()
                            .FirstOrDefault(n => n.Text == "Windows");
                        winNode?.Expand();
                    }
                }

                RefreshCharts();
                RefreshDbGrid();

                // 填充媒体预览列表
                var mediaFiles = allFiles
                    .Where(f => f.FileType == "图片")
                    .Take(50)
                    .Select(f => f.FullPath)
                    .ToList();
                _lbMediaFiles!.Items.Clear();
                foreach (var mf in mediaFiles)
                    _lbMediaFiles.Items.Add(mf);
                if (_lbMediaFiles.Items.Count > 0)
                    _lbMediaFiles.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            _lblStatus!.Text = $"数据准备失败: {ex.Message}";
        }
        finally
        {
            _tsProgress!.Visible = false;
        }
    }

    /// <summary>
    /// 截取完整窗口（含标题栏、菜单栏、状态栏），2倍分辨率保证打印清晰度
    /// 使用Form.DrawToBitmap + 临时放大窗体技巧获取高DPI位图
    /// </summary>
    public void CaptureFullWindow()
    {
        try
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screenshots");
            Directory.CreateDirectory(dir);

            // 保存原始大小，临时放大到2倍以获得高分辨率截图
            int origW = Width, origH = Height;
            int capW = Math.Max(origW, 1200);
            int capH = Math.Max(origH, 780);
            // 2倍分辨率用于打印（A4 300DPI -> 有效约200+DPI）
            int scale = 2;
            Size = new Size(capW, capH);
            Refresh();
            Application.DoEvents();
            Thread.Sleep(300);

            var tabNames = new[] { "文件浏览", "文件搜索", "统计图表", "文件同步", "媒体预览", "数据库管理" };

            for (int i = 0; i < _tabControl!.TabCount; i++)
            {
                _tabControl.SelectedIndex = i;
                _tabControl.Refresh();
                Application.DoEvents();
                Thread.Sleep(500);

                // 2倍分辨率位图
                var bmp = new Bitmap(Width * scale, Height * scale);
                bmp.SetResolution(192, 192); // 设置DPI为192（2x标准96DPI）
                using (var g = Graphics.FromImage(bmp))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    // DrawToBitmap生成1x，然后缩放到2x
                    using var bmp1x = new Bitmap(Width, Height);
                    DrawToBitmap(bmp1x, new Rectangle(0, 0, Width, Height));
                    g.DrawImage(bmp1x, new Rectangle(0, 0, Width * scale, Height * scale),
                        0, 0, Width, Height, GraphicsUnit.Pixel);
                }

                string fileName = $"tab{i + 1}_{tabNames[i]}.png";
                bmp.Save(Path.Combine(dir, fileName), System.Drawing.Imaging.ImageFormat.Png);
                bmp.Dispose();
            }

            // 恢复原始大小
            Size = new Size(origW, origH);
            _tabControl.SelectedIndex = 0;

            // 复制到项目screenshots目录
            string projDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "screenshots");
            try
            {
                Directory.CreateDirectory(projDir);
                foreach (var f in Directory.GetFiles(dir))
                    File.Copy(f, Path.Combine(projDir, Path.GetFileName(f)), true);
            }
            catch { }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"截图失败: {ex.Message}", "FileNexus");
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }
        return $"{size:F2} {units[unitIndex]}";
    }

    #endregion
}
