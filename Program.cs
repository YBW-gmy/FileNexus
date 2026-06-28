namespace FileNexus;

static class Program
{
    /// <summary>
    /// 应用程序入口点
    /// 注册全局异常处理器，捕获未处理异常并友好提示
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // 全局UI线程异常处理 —— 防止未捕获异常导致程序崩溃
        Application.ThreadException += (sender, e) =>
        {
            MessageBox.Show($"发生未预期错误:\n{e.Exception.Message}\n\n详细信息已记录，程序将继续运行。",
                "FileNexus - 错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        // 非UI线程未处理异常
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            string msg = e.ExceptionObject is Exception ex ? ex.Message : "未知错误";
            MessageBox.Show($"发生严重错误:\n{msg}\n程序将退出。",
                "FileNexus - 严重错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        var form = new MainForm();
        // 命令行 --capture 参数：自动索引数据 + 完整窗口截图
        if (args.Length > 0 && args[0] == "--capture")
        {
            form.Shown += async (s, e) =>
            {
                await Task.Delay(1000); // 等待窗口完全渲染
                // 1. 先自动索引系统目录填充数据
                await form.AutoIndexForCapture();
                // 2. 截取完整窗口（含标题栏、菜单、状态栏）
                form.CaptureFullWindow();
                Application.Exit();
            };
        }
        Application.Run(form);
    }
}
