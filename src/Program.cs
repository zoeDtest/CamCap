using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Diagnostics;

namespace IoCameraCapture;

internal static class Program
{
    private static readonly string StartupLogPath = Path.Combine(AppContext.BaseDirectory, "startup.log");

    [STAThread]
    private static void Main()
    {
        try
        {
            LogStartup("Main entered.");
            ApplicationConfiguration.Initialize();
            LogStartup("ApplicationConfiguration initialized.");

            using var form = new MainForm();
            LogStartup("MainForm constructed.");
            Application.Run(form);
            LogStartup("Application exited normally.");
        }
        catch (Exception ex)
        {
            LogStartup($"Fatal startup error: {ex}");
            MessageBox.Show(
                $"程序启动失败。{Environment.NewLine}{Environment.NewLine}{ex}",
                "IO 相机抓图",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    public static void LogStartup(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StartupLogPath)!);
            File.AppendAllText(
                StartupLogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }
}

internal sealed class MainForm : Form
{
    private readonly NumericUpDown _cameraCountBox = new() { Minimum = 1, Maximum = 10, Value = 1 };
    private readonly Button _saveConfigButton = new() { Text = "保存配置" };
    private readonly Button _loadConfigButton = new() { Text = "载入配置" };
    private readonly Button _aboutButton = new() { Text = "版本信息" };
    private readonly Panel _scrollHost = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = UiTheme.PageBackColor };
    private readonly TableLayoutPanel _cameraHost = new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        ColumnCount = 1,
        RowCount = 10,
        Padding = new Padding(0),
        Margin = new Padding(0)
    };
    private readonly TextBox _logText = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        BorderStyle = BorderStyle.None,
        Dock = DockStyle.Fill
    };
    private readonly PictureBox _previewBox = new()
    {
        Dock = DockStyle.Fill,
        SizeMode = PictureBoxSizeMode.Zoom,
        BackColor = UiTheme.PreviewBackColor
    };
    private readonly Label _previewLabel = new()
    {
        Text = "暂无抓图",
        Dock = DockStyle.Bottom,
        AutoSize = true,
        Padding = new Padding(12, 10, 12, 12),
        ForeColor = UiTheme.MutedTextColor,
        BackColor = UiTheme.PanelBackColor
    };

    private readonly List<CameraPanel> _cameraPanels = [];

    public MainForm()
    {
        Program.LogStartup("MainForm constructor started.");

        Text = "IO 接入 SDK 相机拍照存图";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1280, 820);
        BackColor = UiTheme.PageBackColor;
        Font = new Font("Microsoft YaHei UI", 9F);

        BuildLayout();
        CreateCameraPanels();
        ApplyTopBarTheme();
        UpdateCameraPanelVisibility();

        Shown += (_, _) =>
        {
            Program.LogStartup("Main form shown.");
            Activate();
            BringToFront();
        };
        FormClosing += (_, _) =>
        {
            foreach (var panel in _cameraPanels)
            {
                panel.DisposeService();
            }
        };

        Program.LogStartup("MainForm constructor finished.");
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = UiTheme.PageBackColor,
            Padding = new Padding(18, 18, 18, 16)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 340));

        var topBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 54,
            BackColor = UiTheme.PanelBackColor,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0, 0, 0, 12)
        };
        topBar.Paint += (_, e) => ControlPaint.DrawBorder(e.Graphics, topBar.ClientRectangle, UiTheme.BorderColor, ButtonBorderStyle.Solid);

        var topTitle = new Label
        {
            Text = "接入相机数量",
            AutoSize = true,
            Location = new Point(12, 14),
            ForeColor = UiTheme.TextColor,
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
        };
        var topHint = new Label
        {
            Text = "最多支持 10 个相机；当选择多个相机时，除第一个外默认折叠。",
            AutoSize = false,
            Location = new Point(150, 16),
            Size = new Size(520, 20),
            ForeColor = UiTheme.MutedTextColor,
            AutoEllipsis = true
        };
        _cameraCountBox.Width = 72;
        _cameraCountBox.Location = new Point(topBar.Width - 90, 11);
        _cameraCountBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _cameraCountBox.ValueChanged += (_, _) => UpdateCameraPanelVisibility();

        _saveConfigButton.Size = new Size(100, 30);
        _loadConfigButton.Size = new Size(100, 30);
        _aboutButton.Size = new Size(100, 30);
        _saveConfigButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _loadConfigButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _aboutButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _saveConfigButton.Click += (_, _) => SaveConfiguration();
        _loadConfigButton.Click += (_, _) => LoadConfiguration();
        _aboutButton.Click += (_, _) => ShowAboutDialog();
        topBar.Resize += (_, _) =>
        {
            _cameraCountBox.Location = new Point(topBar.Width - 90, 11);
            _loadConfigButton.Location = new Point(topBar.Width - 200, 10);
            _saveConfigButton.Location = new Point(topBar.Width - 308, 10);
            _aboutButton.Location = new Point(topBar.Width - 416, 10);
            topHint.Width = Math.Max(220, topBar.Width - 578);
        };
        _loadConfigButton.Location = new Point(topBar.Width - 200, 10);
        _saveConfigButton.Location = new Point(topBar.Width - 308, 10);
        _aboutButton.Location = new Point(topBar.Width - 416, 10);
        topHint.Width = Math.Max(220, topBar.Width - 578);

        topBar.Controls.Add(topTitle);
        topBar.Controls.Add(topHint);
        topBar.Controls.Add(_aboutButton);
        topBar.Controls.Add(_saveConfigButton);
        topBar.Controls.Add(_loadConfigButton);
        topBar.Controls.Add(_cameraCountBox);

        for (var i = 0; i < 10; i++)
        {
            _cameraHost.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        _scrollHost.Padding = new Padding(0, 0, 0, 12);
        _scrollHost.Controls.Add(_cameraHost);
        _scrollHost.Resize += (_, _) => _cameraHost.Width = Math.Max(780, _scrollHost.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 6);

        var lower = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BackColor = UiTheme.PageBackColor
        };
        lower.Resize += (_, _) => EnsureLowerSplitDistance(lower);

        var logPanel = UiTheme.CreateContainer("运行日志");
        _logText.Font = new Font("Consolas", 10F);
        _logText.BackColor = Color.FromArgb(249, 250, 252);
        _logText.ForeColor = Color.FromArgb(44, 62, 80);
        logPanel.Controls.Add(new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = _logText.BackColor,
            Padding = new Padding(12),
            Controls = { _logText }
        });

        var previewPanel = UiTheme.CreateContainer("最新抓图预览");
        previewPanel.Controls.Add(_previewBox);
        previewPanel.Controls.Add(_previewLabel);

        lower.Panel1.Controls.Add(logPanel);
        lower.Panel2.Controls.Add(previewPanel);
        EnsureLowerSplitDistance(lower);

        root.Controls.Add(topBar, 0, 0);
        root.Controls.Add(_scrollHost, 0, 1);
        root.Controls.Add(lower, 0, 2);
        Controls.Add(root);
    }

    private void ApplyTopBarTheme()
    {
        UiTheme.StyleControl(_aboutButton);
        UiTheme.StyleControl(_saveConfigButton);
        UiTheme.StyleControl(_loadConfigButton);
        UiTheme.StyleNeutralButton(_aboutButton);
        UiTheme.StyleNeutralButton(_saveConfigButton);
        UiTheme.StyleNeutralButton(_loadConfigButton);
    }

    private void CreateCameraPanels()
    {
        for (var i = 1; i <= 10; i++)
        {
            var panel = new CameraPanel(i);
            panel.Dock = DockStyle.Top;
            panel.Margin = new Padding(0, 0, 0, 12);
            panel.LogGenerated += HandleCameraLog;
            panel.CaptureSaved += HandleCameraCaptureSaved;
            panel.CopyTemplateRequested += HandleCopyTemplateRequested;

            _cameraPanels.Add(panel);
            _cameraHost.Controls.Add(panel, 0, i - 1);
        }
    }

    private void UpdateCameraPanelVisibility()
    {
        var count = (int)_cameraCountBox.Value;
        for (var i = 0; i < _cameraPanels.Count; i++)
        {
            var visible = i < count;
            _cameraPanels[i].Visible = visible;
            if (visible)
            {
                _cameraPanels[i].SetExpanded(i == 0 || count == 1);
            }
            else
            {
                _cameraPanels[i].StopRunningWork();
            }
        }
    }

    private void SaveConfiguration()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "保存相机配置",
            Filter = "JSON 配置文件|*.json",
            FileName = "camera-config.json",
            InitialDirectory = AppContext.BaseDirectory
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var config = new MultiCameraConfig(
                CameraCount: (int)_cameraCountBox.Value,
                Cameras: _cameraPanels.Select(panel => panel.ExportConfig()).ToList());

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dialog.FileName, json, Encoding.UTF8);
            HandleCameraLog("系统", "配置", $"已保存配置：{dialog.FileName}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "保存配置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadConfiguration()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "载入相机配置",
            Filter = "JSON 配置文件|*.json",
            InitialDirectory = AppContext.BaseDirectory
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(dialog.FileName, Encoding.UTF8);
            var config = JsonSerializer.Deserialize<MultiCameraConfig>(json)
                ?? throw new InvalidOperationException("配置文件内容为空或格式无效。");

            var count = Math.Clamp(config.CameraCount, 1, 10);
            _cameraCountBox.Value = count;

            for (var i = 0; i < _cameraPanels.Count; i++)
            {
                var cameraConfig = i < config.Cameras.Count ? config.Cameras[i] : null;
                if (cameraConfig is not null)
                {
                    _cameraPanels[i].ApplyConfig(cameraConfig);
                }
            }

            UpdateCameraPanelVisibility();
            HandleCameraLog("系统", "配置", $"已载入配置：{dialog.FileName}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "载入配置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowAboutDialog()
    {
        var version = typeof(MainForm).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        var message =
            "CamCapture 正式交付版" + Environment.NewLine + Environment.NewLine +
            $"版本：{version}" + Environment.NewLine +
            $"产品：CamCapture" + Environment.NewLine +
            $"公司：Imaging" + Environment.NewLine +
            $"运行目录：{AppContext.BaseDirectory}" + Environment.NewLine + Environment.NewLine +
            "功能包括：" + Environment.NewLine +
            "- 多相机配置" + Environment.NewLine +
            "- 独立存图目录与日期分组" + Environment.NewLine +
            "- 配置保存 / 载入" + Environment.NewLine +
            "- 模板复制" + Environment.NewLine +
            "- 分段抓图日志与去抖建议";

        MessageBox.Show(this, message, "版本信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void HandleCameraLog(string cameraName, string source, string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<string, string, string>(HandleCameraLog), cameraName, source, message);
            return;
        }

        _logText.AppendText($"[{DateTime.Now:HH:mm:ss}] [{cameraName}] {source}: {message}{Environment.NewLine}");
    }

    private void HandleCameraCaptureSaved(string cameraName, string path)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<string, string>(HandleCameraCaptureSaved), cameraName, path);
            return;
        }

        var previewStopwatch = Stopwatch.StartNew();
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var image = Image.FromStream(stream);
            var old = _previewBox.Image;
            _previewBox.Image = new Bitmap(image);
            old?.Dispose();
            _previewLabel.Text = $"{cameraName}: {path}";
            previewStopwatch.Stop();
            HandleCameraLog(cameraName, "预览", $"预览已刷新，耗时={previewStopwatch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            previewStopwatch.Stop();
            HandleCameraLog(cameraName, "预览", $"加载图片失败：{ex.Message}");
        }
    }

    private void HandleCopyTemplateRequested(int sourceCameraIndex, CameraPanelConfig template)
    {
        var visibleTargets = _cameraPanels
            .Where(panel => panel.Visible && panel != _cameraPanels[sourceCameraIndex - 1])
            .ToList();

        if (visibleTargets.Count == 0)
        {
            MessageBox.Show(this, "当前没有其它已启用相机可接收模板。", "复制模板", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var result = MessageBox.Show(
            this,
            $"将 {sourceCameraIndex} 号相机的当前模板复制到其它 {visibleTargets.Count} 个已启用相机？",
            "复制模板",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question);

        if (result != DialogResult.OK)
        {
            return;
        }

        foreach (var target in visibleTargets)
        {
            var targetConfig = template with { PanelExpanded = target.Visible };
            target.ApplyConfig(targetConfig);
        }

        HandleCameraLog("系统", "模板", $"已将相机 {sourceCameraIndex} 的模板复制到其它已启用相机。");
    }

    private static void EnsureLowerSplitDistance(SplitContainer lower)
    {
        const int panel1Min = 500;
        const int panel2Min = 420;

        var available = lower.Width - panel2Min - lower.SplitterWidth;
        if (available <= panel1Min)
        {
            return;
        }

        var preferred = Math.Max(panel1Min, (int)(lower.Width * 0.58));
        lower.SplitterDistance = Math.Min(preferred, available);
    }
}

internal sealed class CameraPanel : Panel
{
    private readonly int _cameraIndex;
    private readonly Label _titleLabel = new();
    private readonly Label _headerStatusLabel = new() { AutoSize = true };
    private readonly Button _toggleButton = new() { Text = "折叠" };
    private readonly Button _copyTemplateButton = new() { Text = "复制模板" };
    private readonly Panel _bodyPanel = new() { Dock = DockStyle.Top, AutoSize = true, BackColor = UiTheme.PageBackColor };

    private readonly TextBox _ipText = new() { Text = "192.168.1.64" };
    private readonly NumericUpDown _portBox = new() { Minimum = 1, Maximum = 65535, Value = 8000 };
    private readonly TextBox _userText = new() { Text = "admin" };
    private readonly TextBox _passwordText = new() { UseSystemPasswordChar = true };
    private readonly TextBox _commNoText;
    private readonly TextBox _cameraFolderText;
    private readonly NumericUpDown _channelBox = new() { Minimum = 1, Maximum = 512, Value = 1 };

    private readonly ComboBox _pictureQualityBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _pictureSizeBox = new() { Minimum = 0, Maximum = 65535, Value = 255 };
    private readonly TextBox _outputRootText = new() { Text = Path.Combine(AppContext.BaseDirectory, "captures") };
    private readonly Button _browseOutputButton = new() { Text = "浏览..." };

    private readonly ComboBox _triggerModeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _autoTriggerIntervalBox = new() { Minimum = 50, Maximum = 600000, Increment = 100, Value = 1000 };
    private readonly ComboBox _manualTriggerTypeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _manualTriggerCountBox = new() { Minimum = 1, Maximum = 10000, Value = 5 };
    private readonly NumericUpDown _manualTriggerIntervalBox = new() { Minimum = 50, Maximum = 600000, Increment = 100, Value = 1000 };

    private readonly ComboBox _ioProfileBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _ioModelText = new();
    private readonly NumericUpDown _alarmInputBox = new() { Minimum = 0, Maximum = 4096, Value = 1 };
    private readonly NumericUpDown _debounceBox = new() { Minimum = 0, Maximum = 5000, Increment = 10, Value = 100 };
    private readonly NumericUpDown _pulseWidthBox = new() { Minimum = 10, Maximum = 10000, Increment = 10, Value = 100 };
    private readonly TextBox _ioProfileSummaryText = new() { ReadOnly = true, Multiline = true };
    private readonly Button _toggleIoButton = new() { Text = "展开 IO 参数" };
    private readonly Panel _ioDetailPanel = new() { Dock = DockStyle.Top, AutoSize = true, Visible = false, BackColor = UiTheme.PageBackColor };

    private readonly Button _startButton = new() { Text = "启动布防" };
    private readonly Button _stopButton = new() { Text = "停止", Enabled = false };
    private readonly Button _debugButton = new() { Text = "调试抓图", Enabled = false };
    private readonly Button _testStartButton = new() { Text = "开始测试", Enabled = false };
    private readonly Button _testStopButton = new() { Text = "停止测试", Enabled = false };
    private readonly Label _statusLabel = new() { Text = "未连接", AutoSize = true };

    private CameraIoCaptureService? _service;
    private CancellationTokenSource? _testCts;
    private bool _hasStartedOnce;

    public event Action<string, string, string>? LogGenerated;
    public event Action<string, string>? CaptureSaved;
    public event Action<int, CameraPanelConfig>? CopyTemplateRequested;

    public CameraPanel(int cameraIndex)
    {
        _cameraIndex = cameraIndex;
        _commNoText = new TextBox { Text = $"CAM{cameraIndex:D3}" };
        _cameraFolderText = new TextBox { Text = $"Camera{cameraIndex:D2}" };
        _ioModelText.Text = $"IO-{cameraIndex}";

        AutoSize = true;
        BackColor = UiTheme.PageBackColor;

        BuildLayout();
        ApplyChoices();
        ApplyTheme();
        ApplyIoProfile();
        UpdateTriggerModeUi();
        SetExpanded(cameraIndex == 1);
    }

    public void SetExpanded(bool expanded)
    {
        _bodyPanel.Visible = expanded;
        _toggleButton.Text = expanded ? "折叠" : "展开";
    }

    public bool IsActive => _service is not null;

    public void StopRunningWork()
    {
        StopTriggerTest();
        StopService();
    }

    public void DisposeService()
    {
        StopTriggerTest();
        StopService();
        _hasStartedOnce = false;
        SetHeaderState("未启动", UiTheme.DangerColor);
    }

    private string CameraDisplayName => $"相机 {_cameraIndex}";

    private void SetHeaderState(string text, Color color)
    {
        _headerStatusLabel.Text = text;
        _headerStatusLabel.ForeColor = color;
    }

    private void BuildLayout()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 42,
            BackColor = UiTheme.PanelBackColor,
            Padding = new Padding(12, 4, 12, 4)
        };
        header.Paint += (_, e) => ControlPaint.DrawBorder(e.Graphics, header.ClientRectangle, UiTheme.BorderColor, ButtonBorderStyle.Solid);

        _titleLabel.Text = CameraDisplayName;
        _titleLabel.AutoSize = true;
        _titleLabel.Location = new Point(12, 11);
        _titleLabel.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        _titleLabel.ForeColor = UiTheme.TextColor;

        _headerStatusLabel.Text = "未启动";
        _headerStatusLabel.Location = new Point(88, 11);
        _headerStatusLabel.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        _headerStatusLabel.ForeColor = UiTheme.DangerColor;

        _copyTemplateButton.Size = new Size(96, 28);
        _copyTemplateButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _copyTemplateButton.Click += (_, _) => CopyTemplateRequested?.Invoke(_cameraIndex, ExportConfig());

        _toggleButton.Size = new Size(90, 28);
        _toggleButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _toggleButton.Location = new Point(header.Width - 102, 6);
        _toggleButton.Click += (_, _) => SetExpanded(!_bodyPanel.Visible);
        header.Resize += (_, _) =>
        {
            _toggleButton.Location = new Point(header.Width - 102, 6);
            _copyTemplateButton.Location = new Point(header.Width - 206, 6);
        };
        _copyTemplateButton.Location = new Point(header.Width - 206, 6);

        header.Controls.Add(_titleLabel);
        header.Controls.Add(_headerStatusLabel);
        header.Controls.Add(_copyTemplateButton);
        header.Controls.Add(_toggleButton);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = UiTheme.PageBackColor
        };
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        content.Controls.Add(BuildCommunicationGroup(), 0, 0);
        content.Controls.Add(BuildImageGroup(), 0, 1);
        content.Controls.Add(BuildTriggerGroup(), 0, 2);
        content.Controls.Add(BuildIoGroup(), 0, 3);
        content.Controls.Add(BuildActionBar(), 0, 4);

        _bodyPanel.Controls.Add(content);

        Controls.Add(_bodyPanel);
        Controls.Add(header);
    }

    private Control BuildCommunicationGroup()
    {
        var group = UiTheme.CreateGroup("通讯参数", 4);
        UiTheme.AddLabeled(group, "设备 IP", _ipText, 0, 0);
        UiTheme.AddLabeled(group, "端口", _portBox, 1, 0);
        UiTheme.AddLabeled(group, "用户名", _userText, 2, 0);
        UiTheme.AddLabeled(group, "密码", _passwordText, 3, 0);
        UiTheme.AddLabeled(group, "通讯号码", _commNoText, 0, 1);
        UiTheme.AddLabeled(group, "抓图通道", _channelBox, 1, 1);
        UiTheme.AddLabeled(group, "SDK 目录", UiTheme.CreateReadOnlyText(Path.Combine(AppContext.BaseDirectory, "native")), 2, 1, 2);
        return group;
    }

    private Control BuildImageGroup()
    {
        var group = UiTheme.CreateGroup("图片规格", 4);
        UiTheme.AddLabeled(group, "图片质量", _pictureQualityBox, 0, 0);
        UiTheme.AddLabeled(group, "图片规格", _pictureSizeBox, 1, 0);
        UiTheme.AddLabeled(group, "相机文件夹", _cameraFolderText, 2, 0);

        var outputPanel = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 4, 12, 8)
        };
        outputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        outputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _outputRootText.Margin = new Padding(0);
        _outputRootText.Dock = DockStyle.Fill;
        _browseOutputButton.Margin = new Padding(8, 0, 0, 0);
        _browseOutputButton.Width = 84;
        _browseOutputButton.Click += (_, _) => BrowseOutputRoot();
        outputPanel.Controls.Add(_outputRootText, 0, 0);
        outputPanel.Controls.Add(_browseOutputButton, 1, 0);

        UiTheme.AddLabeled(group, "存图根目录", outputPanel, 0, 1, 4);
        return group;
    }

    private Control BuildTriggerGroup()
    {
        var group = UiTheme.CreateGroup("触发参数", 4);
        UiTheme.AddLabeled(group, "触发模式", _triggerModeBox, 0, 0);
        UiTheme.AddLabeled(group, "自动间隔毫秒", _autoTriggerIntervalBox, 1, 0);
        UiTheme.AddLabeled(group, "自动触发说明", UiTheme.CreateReadOnlyText("自动模式按设定间隔持续触发，直到点击停止测试。"), 2, 0, 2);
        UiTheme.AddLabeled(group, "手动触发", _manualTriggerTypeBox, 0, 1);
        UiTheme.AddLabeled(group, "触发次数", _manualTriggerCountBox, 1, 1);
        UiTheme.AddLabeled(group, "触发毫秒", _manualTriggerIntervalBox, 2, 1);
        return group;
    }

    private Control BuildIoGroup()
    {
        var container = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = UiTheme.PageBackColor,
            Margin = new Padding(0, 0, 0, 10)
        };

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 38,
            BackColor = UiTheme.PanelBackColor,
            Padding = new Padding(12, 4, 12, 4)
        };
        header.Paint += (_, e) => ControlPaint.DrawBorder(e.Graphics, header.ClientRectangle, UiTheme.BorderColor, ButtonBorderStyle.Solid);

        var title = new Label
        {
            Text = "IO 参数",
            AutoSize = true,
            Location = new Point(12, 10),
            ForeColor = UiTheme.TextColor,
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold)
        };

        _toggleIoButton.Size = new Size(120, 28);
        _toggleIoButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _toggleIoButton.Location = new Point(header.Width - 132, 5);
        _toggleIoButton.Click += (_, _) => ToggleIoSection();
        header.Resize += (_, _) => _toggleIoButton.Location = new Point(header.Width - 132, 5);

        header.Controls.Add(title);
        header.Controls.Add(_toggleIoButton);

        var group = UiTheme.CreateGroup("IO 参数", 4);
        UiTheme.AddLabeled(group, "IO 类型", _ioProfileBox, 0, 0);
        UiTheme.AddLabeled(group, "输入号", _alarmInputBox, 1, 0);
        UiTheme.AddLabeled(group, "IO 型号", _ioModelText, 2, 0);
        UiTheme.AddLabeled(group, "去抖毫秒", _debounceBox, 0, 1);
        UiTheme.AddLabeled(group, "脉冲毫秒", _pulseWidthBox, 1, 1);
        UiTheme.AddLabeled(group, "类型说明", _ioProfileSummaryText, 2, 1, 2);

        _ioDetailPanel.Controls.Add(group);
        container.Controls.Add(_ioDetailPanel);
        container.Controls.Add(header);
        return container;
    }

    private Control BuildActionBar()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = UiTheme.PageBackColor,
            Padding = new Padding(4, 4, 4, 10),
            WrapContents = false
        };

        var statusTitle = new Label
        {
            Text = "当前状态",
            AutoSize = true,
            Margin = new Padding(18, 8, 4, 0),
            ForeColor = UiTheme.MutedTextColor
        };
        _statusLabel.Margin = new Padding(0, 8, 0, 0);
        _statusLabel.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);

        _startButton.Click += (_, _) => StartService();
        _stopButton.Click += (_, _) => StopService();
        _debugButton.Click += (_, _) => DebugCapture();
        _testStartButton.Click += (_, _) => StartTriggerTest();
        _testStopButton.Click += (_, _) => StopTriggerTest();

        panel.Controls.AddRange([_startButton, _stopButton, _debugButton, _testStartButton, _testStopButton, statusTitle, _statusLabel]);
        return panel;
    }

    private void ApplyChoices()
    {
        _pictureQualityBox.Items.AddRange(new object[] { "0 - 最佳", "1 - 良好", "2 - 普通" });
        _pictureQualityBox.SelectedIndex = 0;

        _triggerModeBox.Items.AddRange(new object[] { "自动触发", "手动触发" });
        _triggerModeBox.SelectedIndex = 0;
        _triggerModeBox.SelectedIndexChanged += (_, _) => UpdateTriggerModeUi();

        _manualTriggerTypeBox.Items.AddRange(new object[] { "内触发", "外触发" });
        _manualTriggerTypeBox.SelectedIndex = 0;

        _ioProfileBox.DisplayMember = nameof(IoProfile.DisplayName);
        _ioProfileBox.DataSource = IoProfileCatalog.All.ToList();
        _ioProfileBox.SelectedIndexChanged += (_, _) => ApplyIoProfile();
        if (_ioProfileBox.Items.Count > 0)
        {
            _ioProfileBox.SelectedIndex = 0;
        }
    }

    private void ApplyTheme()
    {
        foreach (var control in UiTheme.EnumerateControls(this))
        {
            UiTheme.StyleControl(control);
        }

        UiTheme.StylePrimaryButton(_startButton);
        UiTheme.StyleNeutralButton(_stopButton);
        UiTheme.StyleNeutralButton(_debugButton);
        UiTheme.StylePrimaryButton(_testStartButton);
        UiTheme.StyleNeutralButton(_testStopButton);
        UiTheme.StyleNeutralButton(_browseOutputButton);
        UiTheme.StyleNeutralButton(_toggleButton);
        UiTheme.StyleNeutralButton(_toggleIoButton);
        UiTheme.StyleNeutralButton(_copyTemplateButton);

        _ioProfileSummaryText.Height = 56;
        _ioProfileSummaryText.ScrollBars = ScrollBars.Vertical;
        _statusLabel.ForeColor = UiTheme.DangerColor;
    }

    private void BrowseOutputRoot()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择相机存图根目录",
            SelectedPath = _outputRootText.Text
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _outputRootText.Text = dialog.SelectedPath;
        }
    }

    private void ToggleIoSection()
    {
        _ioDetailPanel.Visible = !_ioDetailPanel.Visible;
        _toggleIoButton.Text = _ioDetailPanel.Visible ? "折叠 IO 参数" : "展开 IO 参数";
    }

    private void UpdateTriggerModeUi()
    {
        var autoMode = _triggerModeBox.SelectedIndex == 0;
        _autoTriggerIntervalBox.Enabled = autoMode;
        _manualTriggerTypeBox.Enabled = !autoMode;
        _manualTriggerCountBox.Enabled = !autoMode;
        _manualTriggerIntervalBox.Enabled = !autoMode;
    }

    private void ApplyIoProfile()
    {
        if (_ioProfileBox.SelectedItem is not IoProfile profile)
        {
            return;
        }

        _ioProfileSummaryText.Text = profile.Summary;
        _debounceBox.Value = Math.Clamp(profile.DefaultDebounceMs, (int)_debounceBox.Minimum, (int)_debounceBox.Maximum);
        _pulseWidthBox.Value = Math.Clamp(profile.DefaultPulseWidthMs, (int)_pulseWidthBox.Minimum, (int)_pulseWidthBox.Maximum);
        if (string.IsNullOrWhiteSpace(_ioModelText.Text) || _ioModelText.Text == $"IO-{_cameraIndex}")
        {
            _ioModelText.Text = profile.ShortCode;
        }
    }

    private void StartService()
    {
        if (_service is not null)
        {
            return;
        }

        try
        {
            _service = new CameraIoCaptureService(ReadOptions());
            _service.Logged += OnServiceLogged;
            _service.Captured += OnServiceCaptured;
            _service.Start();
            _hasStartedOnce = true;
            SetRunningState(true);
            EmitLog("界面", "布防成功，等待 IO 触发。");
        }
        catch (Exception ex)
        {
            if (_service is not null)
            {
                _service.Logged -= OnServiceLogged;
                _service.Captured -= OnServiceCaptured;
                _service.Dispose();
                _service = null;
            }

            SetRunningState(false);
            EmitLog("错误", ex.Message);
            MessageBox.Show(ex.Message, $"{CameraDisplayName} 启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void StopService()
    {
        if (_service is null)
        {
            return;
        }

        StopTriggerTest();
        _service.Logged -= OnServiceLogged;
        _service.Captured -= OnServiceCaptured;
        _service.Dispose();
        _service = null;
        SetRunningState(false);
        SetHeaderState(_hasStartedOnce ? "已暂停" : "未启动", _hasStartedOnce ? Color.FromArgb(208, 132, 0) : UiTheme.DangerColor);
        EmitLog("界面", "已停止。");
    }

    private void DebugCapture()
    {
        try
        {
            var triggerTime = DateTime.Now;
            _service?.Capture("debug_signal", triggerTime, "手动调试抓图");
        }
        catch (Exception ex)
        {
            EmitLog("错误", ex.Message);
        }
    }

    private async void StartTriggerTest()
    {
        if (_service is null || _testCts is not null)
        {
            return;
        }

        _testCts = new CancellationTokenSource();
        SetTestingState(true);

        try
        {
            if (_triggerModeBox.SelectedIndex == 0)
            {
                var intervalMs = (int)_autoTriggerIntervalBox.Value;
                EmitLog("测试", $"已开始自动触发。间隔={intervalMs}ms");
                var index = 1;
                while (true)
                {
                    _testCts.Token.ThrowIfCancellationRequested();
                    var triggerTime = DateTime.Now;
                    _service.Capture($"auto_{index:D4}", triggerTime, "自动触发");
                    index++;
                    await Task.Delay(intervalMs, _testCts.Token);
                }
            }
            else
            {
                var count = (int)_manualTriggerCountBox.Value;
                var intervalMs = (int)_manualTriggerIntervalBox.Value;
                var isExternal = _manualTriggerTypeBox.SelectedIndex == 1;

                EmitLog("测试", $"已开始手动触发。类型={_manualTriggerTypeBox.Text}，次数={count}，触发毫秒={intervalMs}");
                for (var i = 1; i <= count; i++)
                {
                    _testCts.Token.ThrowIfCancellationRequested();

                    if (isExternal)
                    {
                        _service.SimulateExternalTrigger(i);
                    }
                    else
                    {
                        var triggerTime = DateTime.Now;
                        _service.Capture($"manual_{i:D4}", triggerTime, "手动内触发");
                    }

                    if (i < count)
                    {
                        await Task.Delay(intervalMs, _testCts.Token);
                    }
                }

                EmitLog("测试", "手动触发已完成。");
            }
        }
        catch (OperationCanceledException)
        {
            EmitLog("测试", "已停止。");
        }
        catch (Exception ex)
        {
            EmitLog("错误", ex.Message);
        }
        finally
        {
            _testCts?.Dispose();
            _testCts = null;
            SetTestingState(false);
        }
    }

    private void StopTriggerTest()
    {
        _testCts?.Cancel();
    }

    private void SetRunningState(bool running)
    {
        _startButton.Enabled = !running;
        _stopButton.Enabled = running;
        _debugButton.Enabled = running;
        _testStartButton.Enabled = running && _testCts is null;
        _testStopButton.Enabled = running && _testCts is not null;
        _statusLabel.Text = running ? "已连接 / 已布防" : (_hasStartedOnce ? "已暂停" : "未连接");
        _statusLabel.ForeColor = running ? UiTheme.SuccessColor : (_hasStartedOnce ? Color.FromArgb(208, 132, 0) : UiTheme.DangerColor);
        if (running)
        {
            SetHeaderState("已启动", UiTheme.SuccessColor);
        }
    }

    private void SetTestingState(bool testing)
    {
        _testStartButton.Enabled = _service is not null && !testing;
        _testStopButton.Enabled = _service is not null && testing;
        _triggerModeBox.Enabled = !testing;
        if (_service is not null)
        {
            SetHeaderState(testing ? "测试中" : "已启动", testing ? Color.FromArgb(208, 132, 0) : UiTheme.SuccessColor);
        }
        else
        {
            SetHeaderState("未启动", UiTheme.DangerColor);
        }
        if (testing)
        {
            _autoTriggerIntervalBox.Enabled = false;
            _manualTriggerTypeBox.Enabled = false;
            _manualTriggerCountBox.Enabled = false;
            _manualTriggerIntervalBox.Enabled = false;
            return;
        }

        UpdateTriggerModeUi();
    }

    public CameraPanelConfig ExportConfig()
    {
        return new CameraPanelConfig(
            Ip: _ipText.Text.Trim(),
            Port: (ushort)_portBox.Value,
            User: _userText.Text.Trim(),
            Password: _passwordText.Text,
            CommunicationNo: _commNoText.Text.Trim(),
            Channel: (int)_channelBox.Value,
            PictureQuality: _pictureQualityBox.SelectedIndex,
            PictureSize: (ushort)_pictureSizeBox.Value,
            OutputRootDir: _outputRootText.Text.Trim(),
            CameraFolder: _cameraFolderText.Text.Trim(),
            TriggerMode: _triggerModeBox.SelectedIndex,
            AutoTriggerIntervalMs: (int)_autoTriggerIntervalBox.Value,
            ManualTriggerType: _manualTriggerTypeBox.SelectedIndex,
            ManualTriggerCount: (int)_manualTriggerCountBox.Value,
            ManualTriggerIntervalMs: (int)_manualTriggerIntervalBox.Value,
            IoProfileKey: (_ioProfileBox.SelectedItem as IoProfile)?.Key ?? "custom",
            IoModel: _ioModelText.Text.Trim(),
            AlarmInput: (uint)_alarmInputBox.Value,
            DebounceMs: (int)_debounceBox.Value,
            PulseWidthMs: (int)_pulseWidthBox.Value,
            IoExpanded: _ioDetailPanel.Visible,
            PanelExpanded: _bodyPanel.Visible);
    }

    public void ApplyConfig(CameraPanelConfig config)
    {
        StopRunningWork();

        _ipText.Text = config.Ip;
        _portBox.Value = Math.Clamp(config.Port, (ushort)_portBox.Minimum, (ushort)_portBox.Maximum);
        _userText.Text = config.User;
        _passwordText.Text = config.Password;
        _commNoText.Text = config.CommunicationNo;
        _channelBox.Value = Math.Clamp(config.Channel, (int)_channelBox.Minimum, (int)_channelBox.Maximum);
        _pictureQualityBox.SelectedIndex = Math.Clamp(config.PictureQuality, 0, _pictureQualityBox.Items.Count - 1);
        _pictureSizeBox.Value = Math.Clamp(config.PictureSize, (ushort)_pictureSizeBox.Minimum, (ushort)_pictureSizeBox.Maximum);
        _outputRootText.Text = config.OutputRootDir;
        _cameraFolderText.Text = config.CameraFolder;
        _triggerModeBox.SelectedIndex = Math.Clamp(config.TriggerMode, 0, _triggerModeBox.Items.Count - 1);
        _autoTriggerIntervalBox.Value = Math.Clamp(config.AutoTriggerIntervalMs, (int)_autoTriggerIntervalBox.Minimum, (int)_autoTriggerIntervalBox.Maximum);
        _manualTriggerTypeBox.SelectedIndex = Math.Clamp(config.ManualTriggerType, 0, _manualTriggerTypeBox.Items.Count - 1);
        _manualTriggerCountBox.Value = Math.Clamp(config.ManualTriggerCount, (int)_manualTriggerCountBox.Minimum, (int)_manualTriggerCountBox.Maximum);
        _manualTriggerIntervalBox.Value = Math.Clamp(config.ManualTriggerIntervalMs, (int)_manualTriggerIntervalBox.Minimum, (int)_manualTriggerIntervalBox.Maximum);
        _ioModelText.Text = config.IoModel;
        _alarmInputBox.Value = Math.Clamp(config.AlarmInput, (uint)_alarmInputBox.Minimum, (uint)_alarmInputBox.Maximum);
        _debounceBox.Value = Math.Clamp(config.DebounceMs, (int)_debounceBox.Minimum, (int)_debounceBox.Maximum);
        _pulseWidthBox.Value = Math.Clamp(config.PulseWidthMs, (int)_pulseWidthBox.Minimum, (int)_pulseWidthBox.Maximum);

        var profileIndex = IoProfileCatalog.All
            .Select((profile, index) => new { profile.Key, Index = index })
            .FirstOrDefault(item => string.Equals(item.Key, config.IoProfileKey, StringComparison.OrdinalIgnoreCase))
            ?.Index ?? 0;
        _ioProfileBox.SelectedIndex = profileIndex;

        _ioDetailPanel.Visible = config.IoExpanded;
        _toggleIoButton.Text = _ioDetailPanel.Visible ? "折叠 IO 参数" : "展开 IO 参数";
        SetExpanded(config.PanelExpanded);
        UpdateTriggerModeUi();
    }

    private CaptureOptions ReadOptions()
    {
        return new CaptureOptions(
            CameraName: CameraDisplayName,
            CameraFolder: _cameraFolderText.Text.Trim(),
            Ip: _ipText.Text.Trim(),
            Port: (ushort)_portBox.Value,
            User: _userText.Text.Trim(),
            Password: _passwordText.Text,
            CommunicationNo: _commNoText.Text.Trim(),
            IoModel: _ioModelText.Text.Trim(),
            IoProfile: (IoProfile)_ioProfileBox.SelectedItem!,
            DebounceMs: (int)_debounceBox.Value,
            PulseWidthMs: (int)_pulseWidthBox.Value,
            Channel: (int)_channelBox.Value,
            AlarmInput: (uint)_alarmInputBox.Value,
            PictureSize: (ushort)_pictureSizeBox.Value,
            PictureQuality: (ushort)_pictureQualityBox.SelectedIndex,
            OutputRootDir: Path.GetFullPath(_outputRootText.Text.Trim()),
            LogDir: Path.Combine(AppContext.BaseDirectory, "SdkLog"),
            SdkDir: Path.Combine(AppContext.BaseDirectory, "native"));
    }

    private void OnServiceLogged(string source, string message) => EmitLog(source, message);

    private void OnServiceCaptured(string path) => CaptureSaved?.Invoke(CameraDisplayName, path);

    private void EmitLog(string source, string message) => LogGenerated?.Invoke(CameraDisplayName, source, message);
}

internal static class UiTheme
{
    public static readonly Color PageBackColor = Color.FromArgb(245, 247, 250);
    public static readonly Color PanelBackColor = Color.White;
    public static readonly Color PreviewBackColor = Color.FromArgb(248, 249, 252);
    public static readonly Color BorderColor = Color.FromArgb(220, 226, 232);
    public static readonly Color TextColor = Color.FromArgb(35, 48, 61);
    public static readonly Color MutedTextColor = Color.FromArgb(90, 103, 117);
    public static readonly Color SuccessColor = Color.FromArgb(24, 144, 84);
    public static readonly Color DangerColor = Color.FromArgb(220, 53, 69);
    private static readonly Color PrimaryColor = Color.FromArgb(38, 102, 255);
    private static readonly Color PrimaryHoverColor = Color.FromArgb(26, 87, 230);
    private static readonly Color NeutralButtonColor = Color.FromArgb(242, 245, 249);
    private static readonly Color NeutralHoverColor = Color.FromArgb(232, 237, 244);
    private static readonly Color InputBackColor = Color.FromArgb(250, 251, 253);

    public static GroupBox CreateGroup(string title, int valueColumns)
    {
        var group = new GroupBox
        {
            Text = title,
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = PanelBackColor,
            ForeColor = TextColor,
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            Padding = new Padding(12, 10, 12, 12),
            Margin = new Padding(0, 0, 0, 10)
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = valueColumns * 2,
            RowCount = 3,
            Padding = new Padding(12, 10, 12, 12),
            BackColor = PanelBackColor
        };

        for (var i = 0; i < valueColumns; i++)
        {
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / valueColumns));
        }

        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        group.Controls.Add(table);
        return group;
    }

    public static Panel CreateContainer(string title)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelBackColor,
            Padding = new Padding(1)
        };
        panel.Paint += (_, e) => ControlPaint.DrawBorder(e.Graphics, panel.ClientRectangle, BorderColor, ButtonBorderStyle.Solid);

        var header = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 42,
            Padding = new Padding(12, 11, 12, 0),
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            ForeColor = TextColor,
            BackColor = PanelBackColor
        };
        panel.Controls.Add(header);
        panel.Controls.SetChildIndex(header, 0);
        return panel;
    }

    public static TextBox CreateReadOnlyText(string text)
    {
        return new TextBox
        {
            Text = text,
            ReadOnly = true
        };
    }

    public static void AddLabeled(GroupBox group, string labelText, Control control, int pairColumn, int row, int valueSpan = 1)
    {
        var table = (TableLayoutPanel)group.Controls[0];
        var label = new Label
        {
            Text = labelText,
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 9, 6, 0),
            ForeColor = TextColor,
            Font = new Font("Microsoft YaHei UI", 9F),
            AutoEllipsis = true
        };

        var labelColumn = pairColumn * 2;
        var valueColumn = labelColumn + 1;
        table.Controls.Add(label, labelColumn, row);
        table.Controls.Add(control, valueColumn, row);
        table.SetColumnSpan(control, valueSpan * 2 - 1);
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(4, 4, 12, 8);
    }

    public static IEnumerable<Control> EnumerateControls(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            yield return control;
            foreach (var child in EnumerateControls(control))
            {
                yield return child;
            }
        }
    }

    public static void StyleControl(Control control)
    {
        switch (control)
        {
            case TextBox textBox:
                textBox.BorderStyle = BorderStyle.FixedSingle;
                textBox.BackColor = textBox.ReadOnly ? Color.FromArgb(247, 249, 252) : InputBackColor;
                textBox.ForeColor = Color.FromArgb(38, 50, 56);
                break;
            case NumericUpDown numeric:
                numeric.BorderStyle = BorderStyle.FixedSingle;
                numeric.BackColor = InputBackColor;
                numeric.ForeColor = Color.FromArgb(38, 50, 56);
                break;
            case ComboBox combo:
                combo.FlatStyle = FlatStyle.Flat;
                combo.BackColor = InputBackColor;
                combo.ForeColor = Color.FromArgb(38, 50, 56);
                combo.IntegralHeight = false;
                combo.MaxDropDownItems = 12;
                break;
            case Button button:
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = BorderColor;
                button.Height = 34;
                button.Width = Math.Max(button.Width, 96);
                button.Cursor = Cursors.Hand;
                break;
        }
    }

    public static void StylePrimaryButton(Button button)
    {
        button.BackColor = PrimaryColor;
        button.ForeColor = Color.White;
        button.FlatAppearance.BorderColor = PrimaryColor;
        button.MouseEnter += (_, _) =>
        {
            if (button.Enabled)
            {
                button.BackColor = PrimaryHoverColor;
            }
        };
        button.MouseLeave += (_, _) =>
        {
            if (button.Enabled)
            {
                button.BackColor = PrimaryColor;
            }
        };
    }

    public static void StyleNeutralButton(Button button)
    {
        button.BackColor = NeutralButtonColor;
        button.ForeColor = Color.FromArgb(52, 64, 79);
        button.FlatAppearance.BorderColor = BorderColor;
        button.MouseEnter += (_, _) =>
        {
            if (button.Enabled)
            {
                button.BackColor = NeutralHoverColor;
            }
        };
        button.MouseLeave += (_, _) =>
        {
            if (button.Enabled)
            {
                button.BackColor = NeutralButtonColor;
            }
        };
    }
}

internal sealed class CameraIoCaptureService : IDisposable
{
    private readonly CaptureOptions _options;
    private readonly HikvisionSdk.MsgCallBackV31 _alarmCallback;
    private readonly Queue<double> _recentAcceptedIntervalsMs = new();
    private readonly Queue<double> _recentRejectedIntervalsMs = new();
    private int _userId = -1;
    private int _alarmHandle = -1;
    private int _captureSequence;
    private bool _disposed;
    private DateTime _lastAcceptedTriggerUtc = DateTime.MinValue;
    private DateTime _lastObservedTriggerUtc = DateTime.MinValue;

    public event Action<string, string>? Logged;
    public event Action<string>? Captured;

    public CameraIoCaptureService(CaptureOptions options)
    {
        _options = options;
        _alarmCallback = OnAlarm;
    }

    public void Start()
    {
        Directory.CreateDirectory(_options.OutputRootDir);
        Directory.CreateDirectory(_options.LogDir);

        ConfigureNativeSearchPath();
        ConfigureSdkPath();

        if (!HikvisionSdk.NET_DVR_Init())
        {
            throw LastError("NET_DVR_Init");
        }

        HikvisionSdk.NET_DVR_SetLogToFile(3, _options.LogDir, false);
        SetGeneralConfig();
        Login();
        SetupAlarm();
        Log("IO", $"类型={_options.IoProfile.DisplayName}，说明={_options.IoProfile.Summary}，去抖={_options.DebounceMs}ms");
    }

    public void Capture(string reason, DateTime? triggerLocalTime = null, string? triggerSource = null)
    {
        if (_userId < 0)
        {
            throw new InvalidOperationException("设备尚未登录。");
        }

        var jpeg = new HikvisionSdk.NET_DVR_JPEGPARA
        {
            wPicSize = _options.PictureSize,
            wPicQuality = _options.PictureQuality
        };

        var sequence = Interlocked.Increment(ref _captureSequence);
        var captureTime = DateTime.Now;
        if (triggerLocalTime is DateTime triggerTime)
        {
            var queueDelayMs = (captureTime - triggerTime).TotalMilliseconds;
            Log("分段", $"来源={triggerSource ?? "未知触发"}，触发到进入抓图={queueDelayMs:F0}ms，触发时间={triggerTime:yyyy-MM-dd HH:mm:ss.fff}");
        }
        var cameraFolderName = SanitizeFileName(string.IsNullOrWhiteSpace(_options.CameraFolder) ? _options.CameraName : _options.CameraFolder);
        var filePrefix = SanitizeFileName($"{_options.CommunicationNo}_{_options.IoModel}");
        var dateFolder = Path.Combine(_options.OutputRootDir, cameraFolderName, captureTime.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(dateFolder);

        var fileName = $"{filePrefix}_{captureTime:yyyyMMdd_HHmmss_fff}_{reason}_{sequence:D4}.jpg";
        var filePath = Path.Combine(dateFolder, fileName);
        var stopwatch = Stopwatch.StartNew();

        if (!HikvisionSdk.NET_DVR_CaptureJPEGPicture(_userId, _options.Channel, ref jpeg, filePath))
        {
            stopwatch.Stop();
            Log("抓图", LastErrorMessage("NET_DVR_CaptureJPEGPicture"));
            return;
        }

        stopwatch.Stop();
        Log("分段", $"来源={triggerSource ?? "未知触发"}，SDK 抓图并保存={stopwatch.ElapsedMilliseconds}ms");
        Log("抓图", $"已保存：{filePath}，耗时={stopwatch.ElapsedMilliseconds}ms，时间戳={captureTime:yyyy-MM-dd HH:mm:ss.fff}");
        Captured?.Invoke(filePath);
    }

    public void SimulateExternalTrigger(int sequence)
    {
        var triggerTime = DateTime.Now;
        if (!TryAcceptTrigger("模拟外部触发"))
        {
            return;
        }

        Log("IO", $"模拟外部触发 第 {sequence} 次，输入号={_options.AlarmInput}，类型={_options.IoProfile.ShortCode}");
        Capture($"external_{sequence:D4}", triggerTime, "模拟外部触发");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_alarmHandle >= 0)
        {
            HikvisionSdk.NET_DVR_CloseAlarmChan_V30(_alarmHandle);
            _alarmHandle = -1;
        }

        if (_userId >= 0)
        {
            HikvisionSdk.NET_DVR_Logout(_userId);
            _userId = -1;
        }

        HikvisionSdk.NET_DVR_Cleanup();
        HikvisionSdk.SetDllDirectory(null);
    }

    private void ConfigureNativeSearchPath()
    {
        var sdkDll = Path.Combine(_options.SdkDir, "HCNetSDK.dll");
        if (!File.Exists(sdkDll))
        {
            throw new FileNotFoundException("未找到 HCNetSDK.dll，请检查 SDK 目录。", sdkDll);
        }

        if (!HikvisionSdk.SetDllDirectory(_options.SdkDir))
        {
            throw new InvalidOperationException("设置 DLL 目录失败，请检查 SDK 目录。");
        }
    }

    private void ConfigureSdkPath()
    {
        var nativeDir = _options.SdkDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var sdkPath = new HikvisionSdk.NET_DVR_LOCAL_SDK_PATH
        {
            sPath = nativeDir,
            byRes = new byte[128]
        };

        HikvisionSdk.NET_DVR_SetSDKInitCfg(HikvisionSdk.NetSdkInitCfgSdkPath, ref sdkPath);

        var crypto3 = Path.Combine(nativeDir, "libcrypto-3-x64.dll");
        var ssl3 = Path.Combine(nativeDir, "libssl-3-x64.dll");
        var crypto11 = Path.Combine(nativeDir, "ClientDemoDll", "libcrypto-1_1-x64.dll");
        var ssl11 = Path.Combine(nativeDir, "ClientDemoDll", "libssl-1_1-x64.dll");

        SetSdkInitPath(HikvisionSdk.NetSdkInitCfgLibeayPath, File.Exists(crypto3) ? crypto3 : crypto11);
        SetSdkInitPath(HikvisionSdk.NetSdkInitCfgSsleayPath, File.Exists(ssl3) ? ssl3 : ssl11);
    }

    private static void SetSdkInitPath(int type, string path)
    {
        if (File.Exists(path))
        {
            HikvisionSdk.NET_DVR_SetSDKInitCfg(type, ToFixedAnsiBytes(path, 256));
        }
    }

    private static void SetGeneralConfig()
    {
        var cfg = new HikvisionSdk.NET_DVR_LOCAL_GENERAL_CFG
        {
            byAlarmJsonPictureSeparate = 1,
            byRes = new byte[4],
            byRes1 = new byte[232]
        };

        HikvisionSdk.NET_DVR_SetSDKLocalCfg(HikvisionSdk.NetDvrLocalCfgTypeGeneral, ref cfg);
    }

    private void Login()
    {
        var loginInfo = new HikvisionSdk.NET_DVR_USER_LOGIN_INFO
        {
            sDeviceAddress = ToFixedAnsiBytes(_options.Ip, HikvisionSdk.DeviceAddressMaxLength),
            sUserName = ToFixedAnsiBytes(_options.User, HikvisionSdk.LoginUserNameMaxLength),
            sPassword = ToFixedAnsiBytes(_options.Password, HikvisionSdk.LoginPasswordMaxLength),
            wPort = _options.Port,
            bUseAsynLogin = false,
            byLoginMode = 0,
            byRes3 = new byte[119]
        };

        var deviceInfo = new HikvisionSdk.NET_DVR_DEVICEINFO_V40
        {
            struDeviceV30 = new HikvisionSdk.NET_DVR_DEVICEINFO_V30
            {
                sSerialNumber = new byte[HikvisionSdk.SerialNumberLength],
                byRes2 = new byte[9]
            },
            byRes2 = new byte[235]
        };

        _userId = HikvisionSdk.NET_DVR_Login_V40(ref loginInfo, ref deviceInfo);
        if (_userId < 0)
        {
            throw LastError("NET_DVR_Login_V40");
        }

        Log("登录", $"成功。UserID={_userId}，序列号={ReadNullTerminated(deviceInfo.struDeviceV30.sSerialNumber)}");
    }

    private void SetupAlarm()
    {
        if (!HikvisionSdk.NET_DVR_SetDVRMessageCallBack_V31(_alarmCallback, IntPtr.Zero))
        {
            throw LastError("NET_DVR_SetDVRMessageCallBack_V31");
        }

        var alarmParam = new HikvisionSdk.NET_DVR_SETUPALARM_PARAM
        {
            dwSize = (uint)Marshal.SizeOf<HikvisionSdk.NET_DVR_SETUPALARM_PARAM>(),
            byAlarmInfoType = 1,
            byDeployType = 1,
            byRes1 = new byte[2]
        };

        _alarmHandle = HikvisionSdk.NET_DVR_SetupAlarmChan_V41(_userId, ref alarmParam);
        if (_alarmHandle < 0)
        {
            throw LastError("NET_DVR_SetupAlarmChan_V41");
        }

        Log("布防", $"成功。句柄={_alarmHandle}");
    }

    private bool OnAlarm(int command, ref HikvisionSdk.NET_DVR_ALARMER alarmer, IntPtr alarmInfo, uint bufferLength, IntPtr user)
    {
        try
        {
            if (command != HikvisionSdk.COMM_ALARM_V30 || alarmInfo == IntPtr.Zero)
            {
                Log("报警", $"收到非 IO 报警：command=0x{command:X}，len={bufferLength}");
                return true;
            }

            var callbackTime = DateTime.Now;
            var info = Marshal.PtrToStructure<HikvisionSdk.NET_DVR_ALARMINFO_V30>(alarmInfo);
            Log("IO", $"报警类型={info.dwAlarmType}，输入号={info.dwAlarmInputNumber}，设备={alarmer.sDeviceIP}");

            if (info.dwAlarmType != HikvisionSdk.AlarmTypeSignal)
            {
                return true;
            }

            if (_options.AlarmInput > 0 &&
                info.dwAlarmInputNumber != _options.AlarmInput &&
                info.dwAlarmInputNumber + 1 != _options.AlarmInput)
            {
                Log("IO", $"已忽略输入 {info.dwAlarmInputNumber}；当前筛选输入号为 {_options.AlarmInput}");
                return true;
            }

            if (!TryAcceptTrigger("设备 IO 触发"))
            {
                return true;
            }

            Capture($"io{info.dwAlarmInputNumber}", callbackTime, "设备 IO 触发");
        }
        catch (Exception ex)
        {
            Log("错误", $"报警回调失败：{ex.Message}");
        }

        return true;
    }

    private void Log(string source, string message)
    {
        Logged?.Invoke(source, message);
    }

    private bool TryAcceptTrigger(string source)
    {
        var now = DateTime.UtcNow;
        var observedIntervalMs = _lastObservedTriggerUtc == DateTime.MinValue
            ? (double?)null
            : (now - _lastObservedTriggerUtc).TotalMilliseconds;
        _lastObservedTriggerUtc = now;

        var elapsedMs = (now - _lastAcceptedTriggerUtc).TotalMilliseconds;
        if (_lastAcceptedTriggerUtc != DateTime.MinValue && elapsedMs < _options.DebounceMs)
        {
            if (observedIntervalMs is double rejectedMs)
            {
                RememberInterval(_recentRejectedIntervalsMs, rejectedMs);
            }
            Log("IO", $"{source} 被去抖过滤。已过={elapsedMs:F0}ms，要求={_options.DebounceMs}ms；若有效触发频繁被过滤，可适当调小去抖时间。");
            LogDebounceAdvice(source);
            return false;
        }

        if (_lastAcceptedTriggerUtc != DateTime.MinValue)
        {
            RememberInterval(_recentAcceptedIntervalsMs, elapsedMs);
        }

        if (_lastAcceptedTriggerUtc != DateTime.MinValue)
        {
            Log("IO", $"{source} 通过去抖。与上次有效触发间隔={elapsedMs:F0}ms，当前去抖={_options.DebounceMs}ms");
        }
        else
        {
            Log("IO", $"{source} 首次通过去抖。当前去抖={_options.DebounceMs}ms");
        }
        _lastAcceptedTriggerUtc = now;
        LogDebounceAdvice(source);
        return true;
    }

    private void LogDebounceAdvice(string source)
    {
        if (_recentAcceptedIntervalsMs.Count < 3 && _recentRejectedIntervalsMs.Count < 3)
        {
            return;
        }

        var acceptedMedian = MedianOrNull(_recentAcceptedIntervalsMs);
        var rejectedMedian = MedianOrNull(_recentRejectedIntervalsMs);

        int suggestedMs;
        string reason;

        if (rejectedMedian is double rejected && acceptedMedian is double accepted)
        {
            suggestedMs = (int)Math.Round(Math.Clamp((rejected + accepted) / 2.0, 10, 5000));
            reason = $"最近被过滤间隔中位数={rejected:F0}ms，最近有效间隔中位数={accepted:F0}ms";
        }
        else if (rejectedMedian is double onlyRejected)
        {
            suggestedMs = (int)Math.Round(Math.Clamp(onlyRejected * 0.8, 10, 5000));
            reason = $"最近被过滤间隔中位数={onlyRejected:F0}ms";
        }
        else if (acceptedMedian is double onlyAccepted)
        {
            suggestedMs = (int)Math.Round(Math.Clamp(onlyAccepted * 0.6, 10, 5000));
            reason = $"最近有效间隔中位数={onlyAccepted:F0}ms";
        }
        else
        {
            return;
        }

        var delta = suggestedMs - _options.DebounceMs;
        var action = Math.Abs(delta) <= 10
            ? "当前去抖已接近建议值"
            : delta < 0
                ? $"建议调小到约 {suggestedMs}ms"
                : $"建议调大到约 {suggestedMs}ms";

        Log("去抖建议", $"{source}：当前={_options.DebounceMs}ms，{action}；{reason}");
    }

    private static void RememberInterval(Queue<double> queue, double value)
    {
        queue.Enqueue(value);
        while (queue.Count > 12)
        {
            queue.Dequeue();
        }
    }

    private static double? MedianOrNull(IEnumerable<double> values)
    {
        var ordered = values.OrderBy(v => v).ToArray();
        if (ordered.Length == 0)
        {
            return null;
        }

        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 1
            ? ordered[middle]
            : (ordered[middle - 1] + ordered[middle]) / 2.0;
    }

    private static byte[] ToFixedAnsiBytes(string value, int size)
    {
        var buffer = new byte[size];
        var bytes = Encoding.Default.GetBytes(value);
        Array.Copy(bytes, buffer, Math.Min(bytes.Length, size - 1));
        return buffer;
    }

    private static string ReadNullTerminated(byte[] bytes)
    {
        var length = Array.IndexOf(bytes, (byte)0);
        if (length < 0)
        {
            length = bytes.Length;
        }

        return Encoding.Default.GetString(bytes, 0, length);
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(ch => invalid.Contains(ch) || char.IsWhiteSpace(ch) ? '_' : ch).ToArray();
        var result = new string(chars).Trim('_');
        return string.IsNullOrWhiteSpace(result) ? "capture" : result;
    }

    private static InvalidOperationException LastError(string apiName)
    {
        return new InvalidOperationException(LastErrorMessage(apiName));
    }

    private static string LastErrorMessage(string apiName)
    {
        return $"{apiName} 调用失败。SDK 错误码={HikvisionSdk.NET_DVR_GetLastError()}";
    }
}

internal sealed record CaptureOptions(
    string CameraName,
    string CameraFolder,
    string Ip,
    ushort Port,
    string User,
    string Password,
    string CommunicationNo,
    string IoModel,
    IoProfile IoProfile,
    int DebounceMs,
    int PulseWidthMs,
    int Channel,
    uint AlarmInput,
    ushort PictureSize,
    ushort PictureQuality,
    string OutputRootDir,
    string LogDir,
    string SdkDir);

internal sealed record MultiCameraConfig(
    int CameraCount,
    List<CameraPanelConfig> Cameras);

internal sealed record CameraPanelConfig(
    string Ip,
    ushort Port,
    string User,
    string Password,
    string CommunicationNo,
    int Channel,
    int PictureQuality,
    ushort PictureSize,
    string OutputRootDir,
    string CameraFolder,
    int TriggerMode,
    int AutoTriggerIntervalMs,
    int ManualTriggerType,
    int ManualTriggerCount,
    int ManualTriggerIntervalMs,
    string IoProfileKey,
    string IoModel,
    uint AlarmInput,
    int DebounceMs,
    int PulseWidthMs,
    bool IoExpanded,
    bool PanelExpanded);

internal sealed record IoProfile(
    string Key,
    string DisplayName,
    string ShortCode,
    string Wiring,
    string ActiveState,
    string TriggerEdge,
    int DefaultDebounceMs,
    int DefaultPulseWidthMs)
{
    public string Summary => $"{Wiring}；有效电平={ActiveState}；触发沿={TriggerEdge}";
    public override string ToString() => DisplayName;
}

internal static class IoProfileCatalog
{
    public static readonly IReadOnlyList<IoProfile> All =
    [
        new("dry_no", "干接点常开", "DRY-NO", "干接点常开接线", "闭合有效", "下降沿或闭合事件", 80, 100),
        new("dry_nc", "干接点常闭", "DRY-NC", "干接点常闭接线", "断开有效", "上升沿或断开事件", 120, 100),
        new("npn_no", "NPN 开集电极", "NPN-NO", "开集电极灌电流输入", "低电平有效", "下降沿", 50, 80),
        new("pnp_no", "PNP 开集电极", "PNP-NO", "开集电极拉电流输入", "高电平有效", "上升沿", 50, 80),
        new("ttl_high", "TTL 5V 上升沿", "TTL-RISE", "TTL/CMOS 逻辑输入", "高电平有效", "上升沿", 20, 50),
        new("ttl_low", "TTL 5V 下降沿", "TTL-FALL", "TTL/CMOS 逻辑输入", "低电平有效", "下降沿", 20, 50),
        new("relay_pulse", "继电器脉冲 12/24V", "RELAY-PULSE", "继电器输出接 DI 隔离公共端", "脉冲有效", "脉冲边沿", 150, 200),
        new("custom", "自定义 / 厂商专用", "CUSTOM", "自定义接线", "设备定义", "设备定义", 100, 100)
    ];
}
