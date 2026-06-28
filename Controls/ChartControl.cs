namespace FileNexus.Controls;

/// <summary>
/// 自定义GDI+图表控件 —— 支持饼图和柱状图绘制
/// 使用双缓冲技术消除闪烁，抗锯齿渲染提升视觉效果
/// 渐变色填充和GraphicsPath文字路径增强表现力
/// </summary>
public class ChartControl : UserControl
{
    private Dictionary<string, int> _dataSource = new();
    private readonly Color[] _chartColors =
    {
        Color.FromArgb(65, 140, 240),   // 蓝
        Color.FromArgb(89, 194, 121),   // 绿
        Color.FromArgb(237, 101, 89),   // 红
        Color.FromArgb(255, 184, 77),   // 橙
        Color.FromArgb(155, 89, 208),   // 紫
        Color.FromArgb(77, 201, 206),   // 青
        Color.FromArgb(255, 130, 167),  // 粉
        Color.FromArgb(140, 140, 140),  // 灰
    };
    private int _chartType; // 0=饼图, 1=柱状图

    public ChartControl()
    {
        // 启用双缓冲消除重绘闪烁
        SetStyle(ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.ResizeRedraw, true);
        BackColor = Color.White;
        Font = new Font("微软雅黑", 9F);
    }

    public void SetData(Dictionary<string, int> data, int chartType = 0)
    {
        _dataSource = data;
        _chartType = chartType;
        Invalidate(); // 触发重绘
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        // 抗锯齿 —— 使图形边缘平滑
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        if (_dataSource.Count == 0)
        {
            g.DrawString("暂无数据", Font, Brushes.Gray,
                new RectangleF(0, 0, Width, Height),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            return;
        }

        if (_chartType == 0)
            DrawPieChart(g);
        else
            DrawBarChart(g);
    }

    /// <summary>
    /// 绘制饼图 —— Graphics.FillPie按角度填充扇形
    /// 小比例(<5%)扇区不显示文字标签防止重叠
    /// 使用三角函数计算标签位置
    /// </summary>
    private void DrawPieChart(Graphics g)
    {
        int margin = 20;
        int chartSize = Math.Min(Width, Height) - margin * 2;
        var chartRect = new Rectangle(margin, margin, chartSize, chartSize);
        double total = _dataSource.Values.Sum();
        float startAngle = 0f;
        int colorIndex = 0;

        // 绘制标题
        using var titleFont = new Font("微软雅黑", 11F, FontStyle.Bold);
        g.DrawString("文件类型分布", titleFont, Brushes.Black, margin, 5);

        foreach (var kvp in _dataSource)
        {
            double value = kvp.Value;
            float sweepAngle = (float)(value / total * 360);
            Color color = _chartColors[colorIndex % _chartColors.Length];

            // 渐变画刷填充扇形
            using Brush brush = new SolidBrush(color);
            g.FillPie(brush, chartRect, startAngle, sweepAngle);

            // 白色分割线
            using Pen pen = new(Color.White, 2);
            g.DrawPie(pen, chartRect, startAngle, sweepAngle);

            // 仅对占比>5%的扇区绘制标签
            if (value / total > 0.05)
            {
                float midAngle = startAngle + sweepAngle / 2;
                double radians = midAngle * Math.PI / 180;
                float labelX = (float)(Width / 2.0 + Math.Cos(radians) * chartSize * 0.38);
                float labelY = (float)(Height / 2.0 + Math.Sin(radians) * chartSize * 0.38);

                string label = $"{kvp.Key}\n{kvp.Value}个\n{value / total * 100:F1}%";
                using var labelFont = new Font("微软雅黑", 8F);
                var sz = g.MeasureString(label, labelFont);
                g.DrawString(label, labelFont, Brushes.White,
                    labelX - sz.Width / 2, labelY - sz.Height / 2);
            }
            startAngle += sweepAngle;
            colorIndex++;
        }

        // 图例
        int legendX = chartRect.Right + 20;
        int legendY = 60;
        colorIndex = 0;
        foreach (var kvp in _dataSource)
        {
            Color color = _chartColors[colorIndex % _chartColors.Length];
            using Brush brush = new SolidBrush(color);
            g.FillRectangle(brush, legendX, legendY, 12, 12);
            g.DrawRectangle(Pens.Gray, legendX, legendY, 12, 12);
            g.DrawString($"{kvp.Key} ({kvp.Value})", Font, Brushes.Black, legendX + 18, legendY - 1);
            legendY += 20;
            colorIndex++;
        }
    }

    /// <summary>
    /// 绘制柱状图 —— 使用LinearGradientBrush渐变填充
    /// GraphicsPath.AddString实现渐变文字
    /// </summary>
    private void DrawBarChart(Graphics g)
    {
        int margin = 30;
        int chartLeft = margin + 30;
        int chartBottom = Height - margin - 20;
        int chartTop = margin + 20;
        int chartWidth = Width - chartLeft - margin;
        int chartHeight = chartBottom - chartTop;

        double maxVal = _dataSource.Values.Max();
        int barCount = _dataSource.Count;
        int barWidth = Math.Min(60, chartWidth / barCount - 10);
        int totalGap = chartWidth - barWidth * barCount;
        int gap = barCount > 1 ? totalGap / (barCount - 1) : 0;

        // 绘制标题
        using var titleFont = new Font("微软雅黑", 11F, FontStyle.Bold);
        g.DrawString("文件大小分布 (MB)", titleFont, Brushes.Black, margin, 5);

        int i = 0;
        foreach (var kvp in _dataSource)
        {
            int barHeight = maxVal > 0 ? (int)(kvp.Value / maxVal * chartHeight) : 0;
            int x = chartLeft + i * (barWidth + gap);
            int y = chartBottom - barHeight;
            var barRect = new Rectangle(x, y, barWidth, barHeight);

            Color color = _chartColors[i % _chartColors.Length];
            // 渐变色填充 —— 从上到下由浅变深
            using Brush brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                barRect, Color.FromArgb(180, color), color,
                System.Drawing.Drawing2D.LinearGradientMode.Vertical);
            g.FillRectangle(brush, barRect);
            g.DrawRectangle(Pens.Gray, barRect);

            // 柱顶数值
            string valText = $"{kvp.Value}";
            var sz = g.MeasureString(valText, Font);
            g.DrawString(valText, Font, Brushes.Black,
                x + barWidth / 2 - sz.Width / 2, y - sz.Height - 2);

            // 底部标签
            sz = g.MeasureString(kvp.Key, Font);
            g.DrawString(kvp.Key, Font, Brushes.Black,
                x + barWidth / 2 - sz.Width / 2, chartBottom + 3);
            i++;
        }
    }
}
