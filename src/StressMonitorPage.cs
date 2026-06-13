using System.Diagnostics;

namespace IoCameraCapture;

internal sealed class StressMonitorPage : UserControl
{
    private readonly Func<bool> _cameraRunning;
    private readonly Func<bool> _tcpRunning;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 5000 };
    private readonly Label _overallStatus = new() { AutoSize = true };
    private readonly Label _runtimeValue = new() { AutoSize = true };
    private readonly Label _memoryValue = new() { AutoSize = true };
    private readonly Label _diskValue = new() { AutoSize = true };
    private readonly Label _logValue = new() { AutoSize = true };
    private readonly Label _captureValue = new() { AutoSize = true };
    private readonly Label _ngValue = new() { AutoSize = true };
    private readonly DataGridView _alerts = new();
    private readonly TextBox _eventLog = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None
    };
    private readonly Dictionary<string, MonitorAlert> _activeAlerts = new(StringComparer.OrdinalIgnoreCase);
    private readonly DateTime _startedAt = DateTime.Now;
    private DateTime? _lastCaptureAt;
    private long _captureCount;
    private long _ngCount;
    private long _queueFullCount;
    private long _saveFailureCount;
    private long _sdkFailureCount;
    private long _tcpFailureCount;
    private DateTime? _lastQueueFullAt;
    private DateTime? _lastSaveFailureAt;
    private DateTime? _lastSdkFailureAt;
    private DateTime? _lastTcpFailureAt;
    private long _lastLogBytes;
    private long _lastCaptureCount;
    private long _lastNgCount;
    private DateTime _lastLogSampleAt = DateTime.Now;
    private int _responsiveMode = -1;

    public StressMonitorPage(Func<bool> cameraRunning, Func<bool> tcpRunning)
    {
        _cameraRunning = cameraRunning;
        _tcpRunning = tcpRunning;
        Dock = DockStyle.Fill;
        BackColor = UiTheme.PageBackColor;
        BuildLayout();
        _lastLogBytes = DirectorySize(AppPaths.LogDir) + DirectorySize(AppPaths.SdkLogDir);
        _lastCaptureCount = _captureCount;
        _lastNgCount = _ngCount;
        _lastLogSampleAt = DateTime.Now;
        Evaluate();
        _timer.Tick += (_, _) => Evaluate();
        _timer.Start();
    }

    public void RecordLog(string cameraName, string source, string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<string, string, string>(RecordLog), cameraName, source, message);
            return;
        }

        AnalyzeLog(source, message);
        if (IsImportant(source, message))
        {
            _eventLog.AppendText($"[{DateTime.Now:HH:mm:ss}] [{cameraName}] {source}: {message}{Environment.NewLine}");
            UiLogLimiter.Trim(_eventLog);
        }
    }

    public void Stop() => _timer.Stop();

    private void BuildLayout()
    {
        var summary = UiTheme.CreateGroup("24H 压力监控", 4);
        UiTheme.AddLabeled(summary, "综合状态", _overallStatus, 0, 0);
        UiTheme.AddLabeled(summary, "本次运行时长", _runtimeValue, 1, 0);
        UiTheme.AddLabeled(summary, "进程内存", _memoryValue, 2, 0);
        UiTheme.AddLabeled(summary, "数据盘剩余", _diskValue, 3, 0);
        UiTheme.AddLabeled(summary, "日志总量 / 增速", _logValue, 0, 1);
        UiTheme.AddLabeled(summary, "抓图数量 / 最近抓图", _captureValue, 1, 1);
        UiTheme.AddLabeled(summary, "NG 数量", _ngValue, 2, 1);
        UiTheme.AddLabeled(summary, "监控频率", UiTheme.CreateReadOnlyText("每 5 秒检查一次"), 3, 1);

        _alerts.Dock = DockStyle.Fill;
        _alerts.ReadOnly = true;
        _alerts.AllowUserToAddRows = false;
        _alerts.AllowUserToDeleteRows = false;
        _alerts.AllowUserToResizeColumns = true;
        _alerts.AllowUserToResizeRows = false;
        _alerts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        _alerts.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        _alerts.ScrollBars = ScrollBars.Both;
        _alerts.BorderStyle = BorderStyle.None;
        _alerts.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _alerts.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        _alerts.ColumnHeadersHeight = 40;
        _alerts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _alerts.ColumnHeadersVisible = true;
        _alerts.EnableHeadersVisualStyles = false;
        _alerts.GridColor = UiTheme.BorderColor;
        _alerts.RowTemplate.Height = 38;
        _alerts.RowHeadersVisible = false;
        _alerts.BackgroundColor = UiTheme.PanelBackColor;
        _alerts.DefaultCellStyle.BackColor = UiTheme.PanelBackColor;
        _alerts.DefaultCellStyle.Font = new Font("Microsoft YaHei UI", 10F);
        _alerts.DefaultCellStyle.Padding = new Padding(8, 6, 8, 6);
        _alerts.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 240, 254);
        _alerts.DefaultCellStyle.SelectionForeColor = UiTheme.TextColor;
        _alerts.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        _alerts.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 252);
        _alerts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(242, 245, 249);
        _alerts.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.TextColor;
        _alerts.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        _alerts.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 4, 8, 4);
        _alerts.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        _alerts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Level", HeaderText = "等级", Width = 100, MinimumWidth = 80 });
        _alerts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Parameter", HeaderText = "报警参数", Width = 190, MinimumWidth = 120 });
        _alerts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Current", HeaderText = "当前值", Width = 180, MinimumWidth = 120 });
        _alerts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Threshold", HeaderText = "报警阈值", Width = 300, MinimumWidth = 180 });
        _alerts.Columns.Add(new DataGridViewTextBoxColumn { Name = "Suggestion", HeaderText = "处理建议", Width = 420, MinimumWidth = 240 });

        var alertPanel = UiTheme.CreateContainer("当前报警");
        alertPanel.Controls.Add(_alerts);
        var eventPanel = UiTheme.CreateContainer("异常日志摘要");
        _eventLog.Font = new Font("Consolas", 9F);
        _eventLog.BackColor = Color.FromArgb(249, 250, 252);
        eventPanel.Controls.Add(_eventLog);

        var lower = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            Panel1MinSize = 150,
            Panel2MinSize = 120,
            BackColor = UiTheme.PageBackColor
        };
        lower.Resize += (_, _) => EnsureLowerSplitDistance(lower);
        lower.VisibleChanged += (_, _) => EnsureLowerSplitDistance(lower);
        lower.Panel1.Controls.Add(alertPanel);
        lower.Panel2.Controls.Add(eventPanel);

        var root = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(18),
            BackColor = UiTheme.PageBackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(summary, 0, 0);
        root.Controls.Add(lower, 0, 1);

        var viewport = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = UiTheme.PageBackColor
        };
        viewport.Controls.Add(root);
        viewport.Resize += (_, _) => UpdateResponsiveLayout(viewport, root, summary);
        Controls.Add(viewport);
        UpdateResponsiveLayout(viewport, root, summary);
    }

    private void UpdateResponsiveLayout(Panel viewport, TableLayoutPanel root, GroupBox summary)
    {
        var availableWidth = Math.Max(1, viewport.ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
        var availableHeight = Math.Max(1, viewport.ClientSize.Height - SystemInformation.HorizontalScrollBarHeight);
        var contentWidth = Math.Max(900, availableWidth);
        var contentHeight = Math.Max(560, availableHeight);
        var compact = availableWidth < 1100;
        var fontSize = availableWidth < 900 ? 8.5F : compact ? 9F : 10F;

        root.Location = Point.Empty;
        root.Size = new Size(contentWidth, contentHeight);
        viewport.AutoScrollMinSize = new Size(contentWidth, contentHeight);

        var responsiveMode = availableWidth < 900 ? 0 : compact ? 1 : 2;
        if (_responsiveMode == responsiveMode)
        {
            return;
        }
        _responsiveMode = responsiveMode;

        UpdateAlertColumnLayout(responsiveMode);
        summary.Font = new Font("Microsoft YaHei UI", compact ? 9F : 10F, FontStyle.Bold);
        SetControlFont(summary, new Font("Microsoft YaHei UI", fontSize));
        _alerts.DefaultCellStyle.Font = new Font("Microsoft YaHei UI", fontSize);
        _alerts.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", fontSize, FontStyle.Bold);
        _alerts.ColumnHeadersHeight = compact ? 34 : 40;
        _alerts.DefaultCellStyle.Padding = compact ? new Padding(5, 4, 5, 4) : new Padding(8, 6, 8, 6);
        _eventLog.Font = new Font("Consolas", compact ? 8F : 9F);
    }

    private void UpdateAlertColumnLayout(int responsiveMode)
    {
        if (responsiveMode >= 1)
        {
            _alerts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _alerts.Columns["Level"].FillWeight = 50;
            _alerts.Columns["Parameter"].FillWeight = 90;
            _alerts.Columns["Current"].FillWeight = 75;
            _alerts.Columns["Threshold"].FillWeight = 135;
            _alerts.Columns["Suggestion"].FillWeight = 210;
            return;
        }

        _alerts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        _alerts.Columns["Level"].Width = 80;
        _alerts.Columns["Parameter"].Width = 140;
        _alerts.Columns["Current"].Width = 130;
        _alerts.Columns["Threshold"].Width = 220;
        _alerts.Columns["Suggestion"].Width = 300;
    }

    private static void SetControlFont(Control parent, Font font)
    {
        foreach (Control control in parent.Controls)
        {
            control.Font = font;
            SetControlFont(control, font);
        }
    }

    private static void EnsureLowerSplitDistance(SplitContainer lower)
    {
        var available = lower.Height - lower.SplitterWidth;
        var maximum = available - lower.Panel2MinSize;
        if (maximum < lower.Panel1MinSize)
        {
            return;
        }

        var preferred = (int)(available * 0.55);
        lower.SplitterDistance = Math.Clamp(preferred, lower.Panel1MinSize, maximum);
    }

    private void Evaluate()
    {
        var process = Process.GetCurrentProcess();
        process.Refresh();
        var privateMb = process.PrivateMemorySize64 / 1024d / 1024d;
        var drive = new DriveInfo(Path.GetPathRoot(AppPaths.DataDir)!);
        var freeGb = drive.AvailableFreeSpace / 1024d / 1024d / 1024d;
        var logBytes = DirectorySize(AppPaths.LogDir) + DirectorySize(AppPaths.SdkLogDir);
        var now = DateTime.Now;
        var elapsedMinutes = Math.Max((now - _lastLogSampleAt).TotalMinutes, 0.01);
        var logGrowthMbPerHour = Math.Max(0, logBytes - _lastLogBytes) / 1024d / 1024d / elapsedMinutes * 60d;
        var capturePerSecond = Math.Max(0, _captureCount - _lastCaptureCount) / elapsedMinutes / 60d;
        var ngPerMinute = Math.Max(0, _ngCount - _lastNgCount) / elapsedMinutes;
        _lastLogBytes = logBytes;
        _lastCaptureCount = _captureCount;
        _lastNgCount = _ngCount;
        _lastLogSampleAt = now;

        SetAlert("内存", privateMb >= 1024 ? "严重" : privateMb >= 512 ? "警告" : null,
            $"{privateMb:F0} MB", "警告 512 MB；严重 1024 MB", "检查高频抓图、界面卡顿或内存持续增长。");
        SetAlert("磁盘剩余", freeGb <= 5 ? "严重" : freeGb <= 20 ? "警告" : null,
            $"{freeGb:F1} GB", "警告 20 GB；严重 5 GB", "立即清理日志/图片或更换存储盘。");
        SetAlert("日志总量", logBytes >= 2L * 1024 * 1024 * 1024 ? "严重" : logBytes >= 500L * 1024 * 1024 ? "警告" : null,
            $"{logBytes / 1024d / 1024d:F1} MB", "警告 500 MB；严重 2 GB", "降低详细日志量并清理旧日志。");
        SetAlert("日志增长速度", logGrowthMbPerHour >= 500 ? "严重" : logGrowthMbPerHour >= 100 ? "警告" : null,
            $"{logGrowthMbPerHour:F1} MB/h", "警告 100 MB/h；严重 500 MB/h", "检查持续抓图和持续 NG 是否产生过量日志。");
        SetAlert("抓图断档", _cameraRunning() && (!_lastCaptureAt.HasValue || now - _lastCaptureAt.Value > TimeSpan.FromSeconds(30)) ? "严重" : null,
            _lastCaptureAt.HasValue ? $"{(now - _lastCaptureAt.Value).TotalSeconds:F0} 秒未抓图" : "尚无抓图",
            "相机运行时超过 30 秒", "检查相机连接、SDK 错误码和处理文件夹权限。");
        SetAlert("抓图频率", capturePerSecond >= 10 ? "严重" : capturePerSecond >= 5 ? "警告" : null,
            $"{capturePerSecond:F1} 张/秒", "警告 5 张/秒；严重 10 张/秒", "高频抓图会增加磁盘写入和内存压力，建议增大抓图间隔。");
        SetAlert("NG 上报频率", ngPerMinute >= 600 ? "严重" : ngPerMinute >= 120 ? "警告" : null,
            $"{ngPerMinute:F0} 次/分钟", "警告 120 次/分钟；严重 600 次/分钟", "持续高频 NG 会增加界面和日志压力，检查上游上报频率。");
        SetAlert("写入队列已满", IsRecent(_lastQueueFullAt, now) ? "严重" : null, $"{_queueFullCount} 次", "最近 5 分钟应为 0", "磁盘写入跟不上抓图速度，增大抓图间隔。");
        SetAlert("图片保存失败", IsRecent(_lastSaveFailureAt, now) ? "严重" : null, $"{_saveFailureCount} 次", "最近 5 分钟应为 0", "检查磁盘空间、目录权限和文件占用。");
        SetAlert("相机 SDK 异常", IsRecent(_lastSdkFailureAt, now) ? "严重" : null, $"{_sdkFailureCount} 次", "最近 5 分钟应为 0", "检查网络、相机登录、SDK 错误码，必要时重新连接。");
        SetAlert("TCP 异常", _tcpRunning() && IsRecent(_lastTcpFailureAt, now) ? "警告" : null, $"{_tcpFailureCount} 次", "当前连接正常且最近 5 分钟无异常", "检查 TCP 服务端和网络稳定性。");

        _runtimeValue.Text = FormatDuration(now - _startedAt);
        _memoryValue.Text = $"{privateMb:F0} MB";
        _diskValue.Text = $"{freeGb:F1} GB";
        _logValue.Text = $"{logBytes / 1024d / 1024d:F1} MB / {logGrowthMbPerHour:F1} MB/h";
        _captureValue.Text = $"{_captureCount:N0} / {(_lastCaptureAt.HasValue ? _lastCaptureAt.Value.ToString("HH:mm:ss") : "-")}";
        _ngValue.Text = $"{_ngCount:N0}";
        RefreshAlerts();
    }

    private void AnalyzeLog(string source, string message)
    {
        if (source == "抓图状态" || message.Contains("抓图状态:", StringComparison.Ordinal))
        {
            _captureCount++;
            _lastCaptureAt = DateTime.Now;
        }
        if ((source == "结果" || message.Contains("结果:", StringComparison.Ordinal)) && message.Contains("Ng:", StringComparison.OrdinalIgnoreCase))
        {
            _ngCount++;
        }
        if (message.Contains("异步队列已满", StringComparison.Ordinal))
        {
            _queueFullCount++;
            _lastQueueFullAt = DateTime.Now;
        }
        if (message.Contains("保存失败", StringComparison.Ordinal))
        {
            _saveFailureCount++;
            _lastSaveFailureAt = DateTime.Now;
        }
        if (message.Contains("SDK 错误码", StringComparison.Ordinal) || message.Contains("NET_DVR_", StringComparison.Ordinal) && message.Contains("失败", StringComparison.Ordinal))
        {
            _sdkFailureCount++;
            _lastSdkFailureAt = DateTime.Now;
        }
        if (message.Contains("连接异常", StringComparison.Ordinal) || message.Contains("远端已断开", StringComparison.Ordinal))
        {
            _tcpFailureCount++;
            _lastTcpFailureAt = DateTime.Now;
        }
        if (message.Contains("已连接", StringComparison.Ordinal) && source == "TCP")
        {
            _lastTcpFailureAt = null;
        }
    }

    private void SetAlert(string parameter, string? level, string current, string threshold, string suggestion)
    {
        if (level is null)
        {
            _activeAlerts.Remove(parameter);
            return;
        }
        _activeAlerts[parameter] = new MonitorAlert(level, parameter, current, threshold, suggestion);
    }

    private void RefreshAlerts()
    {
        _alerts.Rows.Clear();
        foreach (var alert in _activeAlerts.Values.OrderBy(alert => alert.Level == "严重" ? 0 : 1).ThenBy(alert => alert.Parameter))
        {
            var index = _alerts.Rows.Add(alert.Level, alert.Parameter, alert.Current, alert.Threshold, alert.Suggestion);
            _alerts.Rows[index].DefaultCellStyle.ForeColor = alert.Level == "严重" ? UiTheme.DangerColor : Color.FromArgb(208, 132, 0);
        }
        _alerts.ClearSelection();
        _overallStatus.Text = _activeAlerts.Values.Any(alert => alert.Level == "严重") ? "严重报警" : _activeAlerts.Count > 0 ? "存在警告" : "正常";
        _overallStatus.ForeColor = _activeAlerts.Values.Any(alert => alert.Level == "严重") ? UiTheme.DangerColor : _activeAlerts.Count > 0 ? Color.FromArgb(208, 132, 0) : UiTheme.SuccessColor;
    }

    private static bool IsImportant(string source, string message) =>
        source is "错误" or "TCP" || message.Contains("失败", StringComparison.Ordinal) || message.Contains("异常", StringComparison.Ordinal) || message.Contains("断开", StringComparison.Ordinal) || message.Contains("异步队列已满", StringComparison.Ordinal);

    private static long DirectorySize(string path)
    {
        try
        {
            return Directory.Exists(path) ? Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Sum(file => new FileInfo(file).Length) : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static string FormatDuration(TimeSpan duration) => $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";

    private static bool IsRecent(DateTime? occurredAt, DateTime now) =>
        occurredAt.HasValue && now - occurredAt.Value <= TimeSpan.FromMinutes(5);

    private sealed record MonitorAlert(string Level, string Parameter, string Current, string Threshold, string Suggestion);
}
