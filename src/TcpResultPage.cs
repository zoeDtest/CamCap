using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace IoCameraCapture;

internal sealed class TcpResultPage : UserControl
{
    private readonly TextBox _hostText = new() { Text = "127.0.0.1" };
    private readonly NumericUpDown _portBox = new() { Minimum = 1, Maximum = 65535, Value = 9000 };
    private readonly CheckBox _startOnLaunchBox = new() { Text = "保存后下次自动连接", Checked = true };
    private readonly ComboBox _parseModeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _delimiterText = new() { Text = "," };
    private readonly NumericUpDown _fieldIndexBox = new() { Minimum = 1, Maximum = 100, Value = 2 };
    private readonly TextBox _resultKeyText = new() { Text = "result" };
    private readonly NumericUpDown _partialTimeoutBox = new() { Minimum = 50, Maximum = 5000, Increment = 50, Value = 150 };
    private readonly NumericUpDown _audioDurationBox = new() { Minimum = 100, Maximum = 30000, Increment = 100, Value = 1500 };
    private readonly Button _startButton = new() { Text = "启动连接" };
    private readonly Button _stopButton = new() { Text = "停止连接" };
    private readonly Button _testOkButton = new() { Text = "测试 OK 音频" };
    private readonly Button _testNgButton = new() { Text = "测试 NG 音频" };
    private readonly Button _clearButton = new() { Text = "清空统计与日志" };
    private readonly Label _connectionStatus = new() { Text = "未连接", AutoSize = true };
    private readonly Label _lastResult = new() { Text = "暂无结果", AutoSize = true };
    private readonly Label _okCountLabel = new() { Text = "0", AutoSize = true };
    private readonly Label _ngCountLabel = new() { Text = "0", AutoSize = true };
    private readonly Label _unknownCountLabel = new() { Text = "0", AutoSize = true };
    private readonly Label _lastReceiveLabel = new() { Text = "-", AutoSize = true };
    private readonly TextBox _lastMessageText = new() { ReadOnly = true };
    private readonly TextBox _logText = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        BorderStyle = BorderStyle.None,
        Dock = DockStyle.Fill
    };

    private TcpMonitorService? _service;
    private int _okCount;
    private int _ngCount;
    private int _unknownCount;

    public event Action<string, string, string>? LogGenerated;
    public event Action<bool, string>? ConnectionStateChanged;

    public TcpResultPage()
    {
        Dock = DockStyle.Fill;
        BackColor = UiTheme.PageBackColor;
        BuildLayout();
        ApplyTheme();
        UpdateParseControls();
        UpdateButtonState(false);
    }

    public TcpMonitorConfig ExportConfig()
    {
        return new TcpMonitorConfig(
            Host: _hostText.Text.Trim(),
            Port: (int)_portBox.Value,
            StartOnLaunch: _startOnLaunchBox.Checked,
            ParseMode: (TcpParseMode)_parseModeBox.SelectedIndex,
            Delimiter: _delimiterText.Text,
            FieldIndex: (int)_fieldIndexBox.Value,
            ResultKey: _resultKeyText.Text.Trim(),
            PartialMessageTimeoutMs: (int)_partialTimeoutBox.Value,
            AudioPlaybackDurationMs: (int)_audioDurationBox.Value);
    }

    public void ApplyConfig(TcpMonitorConfig config)
    {
        Stop();
        _hostText.Text = string.IsNullOrWhiteSpace(config.Host) ? "127.0.0.1" : config.Host;
        _portBox.Value = Math.Clamp(config.Port, 1, 65535);
        _startOnLaunchBox.Checked = config.StartOnLaunch;
        _parseModeBox.SelectedIndex = Math.Clamp((int)config.ParseMode, 0, 2);
        _delimiterText.Text = config.Delimiter ?? ",";
        _fieldIndexBox.Value = Math.Clamp(config.FieldIndex, 1, 100);
        _resultKeyText.Text = string.IsNullOrWhiteSpace(config.ResultKey) ? "result" : config.ResultKey;
        _partialTimeoutBox.Value = Math.Clamp(config.PartialMessageTimeoutMs, 50, 5000);
        _audioDurationBox.Value = Math.Clamp(config.AudioPlaybackDurationMs <= 0 ? 1500 : config.AudioPlaybackDurationMs, 100, 30000);
        UpdateParseControls();

        if (config.StartOnLaunch)
        {
            StartConnection();
        }
    }

    public void StartConnection() => Start();

    public void StopConnection() => Stop();

    public bool IsRunning => _service is not null;

    public void Stop()
    {
        var service = _service;
        _service = null;
        service?.Dispose();
        UpdateConnectionState("未连接", false);
        UpdateButtonState(false);
    }

    private void BuildLayout()
    {
        _parseModeBox.Items.AddRange(["整条报文扫描", "按字段序号", "按键值对"]);
        _parseModeBox.SelectedIndex = 0;
        _parseModeBox.SelectedIndexChanged += (_, _) => UpdateParseControls();

        _startButton.Click += (_, _) => Start();
        _stopButton.Click += (_, _) => Stop();
        _testOkButton.Click += (_, _) => PlayAudio(TcpMatchResult.Ok);
        _testNgButton.Click += (_, _) => PlayAudio(TcpMatchResult.Ng);
        _clearButton.Click += (_, _) => ClearStatistics();

        var settings = UiTheme.CreateGroup("VisionMarker TCP 客户端", 2);
        UiTheme.AddLabeled(settings, "服务端地址", _hostText, 0, 0);
        UiTheme.AddLabeled(settings, "服务端端口", _portBox, 1, 0);
        UiTheme.AddLabeled(settings, "解析模式", _parseModeBox, 0, 1);
        UiTheme.AddLabeled(settings, "残包补帧等待(ms)", _partialTimeoutBox, 1, 1);
        UiTheme.AddLabeled(settings, "字段分隔符", _delimiterText, 0, 2);
        UiTheme.AddLabeled(settings, "结果字段序号", _fieldIndexBox, 1, 2);
        UiTheme.AddLabeled(settings, "结果键名", _resultKeyText, 0, 3);
        UiTheme.AddLabeled(settings, "自动连接", _startOnLaunchBox, 1, 3);
        UiTheme.AddLabeled(settings, "单次音频时长(ms)", _audioDurationBox, 0, 4);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = true,
            Padding = new Padding(0, 10, 0, 10),
            BackColor = UiTheme.PageBackColor
        };
        _startButton.Size = new Size(116, 36);
        _stopButton.Size = new Size(116, 36);
        _testOkButton.Size = new Size(140, 36);
        _testNgButton.Size = new Size(140, 36);
        _clearButton.Size = new Size(156, 36);
        foreach (var button in new[] { _startButton, _stopButton, _testOkButton, _testNgButton, _clearButton })
        {
            button.Margin = new Padding(0, 0, 10, 0);
        }
        toolbar.Controls.AddRange([_startButton, _stopButton, _testOkButton, _testNgButton, _clearButton]);

        var status = UiTheme.CreateGroup("运行状态", 3);
        UiTheme.AddLabeled(status, "连接状态", _connectionStatus, 0, 0);
        UiTheme.AddLabeled(status, "最近结果", _lastResult, 1, 0);
        UiTheme.AddLabeled(status, "最后接收时间", _lastReceiveLabel, 2, 0);
        UiTheme.AddLabeled(status, "OK 数量", _okCountLabel, 0, 1);
        UiTheme.AddLabeled(status, "NG 数量", _ngCountLabel, 1, 1);
        UiTheme.AddLabeled(status, "未识别数量", _unknownCountLabel, 2, 1);
        UiTheme.AddLabeled(status, "最近原始报文", _lastMessageText, 0, 2, 3);

        var upper = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 8),
            BackColor = UiTheme.PageBackColor
        };
        upper.Controls.Add(status);
        upper.Controls.Add(toolbar);
        upper.Controls.Add(settings);

        var logPanel = UiTheme.CreateContainer("TCP 结果日志");
        _logText.Font = new Font("Consolas", 10F);
        _logText.BackColor = Color.FromArgb(249, 250, 252);
        _logText.ForeColor = Color.FromArgb(44, 62, 80);
        var logBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            BackColor = _logText.BackColor,
            Controls = { _logText }
        };
        logPanel.Controls.Add(logBody);
        logPanel.Controls.SetChildIndex(logBody, 1);

        var root = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 18, 18, 16),
            BackColor = UiTheme.PageBackColor
        };
        root.Controls.Add(logPanel);
        root.Controls.Add(upper);
        Controls.Add(root);
    }

    private void ApplyTheme()
    {
        foreach (var control in UiTheme.EnumerateControls(this))
        {
            UiTheme.StyleControl(control);
        }

        UiTheme.StylePrimaryButton(_startButton);
        foreach (var button in new[] { _stopButton, _testOkButton, _testNgButton, _clearButton })
        {
            UiTheme.StyleNeutralButton(button);
        }

        _connectionStatus.ForeColor = UiTheme.MutedTextColor;
        _lastResult.ForeColor = UiTheme.MutedTextColor;
        _okCountLabel.ForeColor = UiTheme.SuccessColor;
        _ngCountLabel.ForeColor = UiTheme.DangerColor;
    }

    private void Start()
    {
        Stop();
        var config = ExportConfig();
        if (string.IsNullOrWhiteSpace(config.Host))
        {
            MessageBox.Show(this, "请输入 VisionMarker 服务端地址。", "TCP 结果监听", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _service = new TcpMonitorService(config);
        _service.Logged += message => PostToUi(() => AppendLog("TCP", message));
        _service.StateChanged += (message, connected) => PostToUi(() => UpdateConnectionState(message, connected));
        _service.MessageReceived += (message, result) => PostToUi(() => HandleMessage(message, result));
        _service.Start();
        UpdateButtonState(true);
        AppendLog("TCP", $"开始连接 {config.Host}:{config.Port}");
    }

    private void HandleMessage(string message, TcpMatchResult result)
    {
        _lastMessageText.Text = message;
        _lastReceiveLabel.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        switch (result)
        {
            case TcpMatchResult.Ok:
                _okCountLabel.Text = (++_okCount).ToString();
                _lastResult.Text = "OK";
                _lastResult.ForeColor = UiTheme.SuccessColor;
                PlayAudio(result);
                break;
            case TcpMatchResult.Ng:
                _ngCountLabel.Text = (++_ngCount).ToString();
                _lastResult.Text = "NG";
                _lastResult.ForeColor = UiTheme.DangerColor;
                PlayAudio(result);
                break;
            default:
                _unknownCountLabel.Text = (++_unknownCount).ToString();
                _lastResult.Text = "未识别";
                _lastResult.ForeColor = UiTheme.MutedTextColor;
                break;
        }

        AppendLog("结果", $"{result}: {message}");
    }

    private void PlayAudio(TcpMatchResult result)
    {
        var fileName = result == TcpMatchResult.Ok ? "ResultOK.wav" : "ResultNG.wav";
        var path = Path.Combine(AppContext.BaseDirectory, "Media", fileName);
        if (!File.Exists(path))
        {
            AppendLog("音频", $"未找到音频文件：{path}");
            return;
        }

        ResultAudioPlayer.Play(path, (int)_audioDurationBox.Value);
    }

    private void ClearStatistics()
    {
        _okCount = 0;
        _ngCount = 0;
        _unknownCount = 0;
        _okCountLabel.Text = "0";
        _ngCountLabel.Text = "0";
        _unknownCountLabel.Text = "0";
        _lastResult.Text = "暂无结果";
        _lastResult.ForeColor = UiTheme.MutedTextColor;
        _lastMessageText.Clear();
        _lastReceiveLabel.Text = "-";
        _logText.Clear();
        AppendLog("界面", "已清空 TCP 统计与界面日志。");
    }

    private void UpdateParseControls()
    {
        var mode = (TcpParseMode)Math.Max(0, _parseModeBox.SelectedIndex);
        _delimiterText.Enabled = mode == TcpParseMode.DelimitedField;
        _fieldIndexBox.Enabled = mode == TcpParseMode.DelimitedField;
        _resultKeyText.Enabled = mode == TcpParseMode.KeyValue;
    }

    private void UpdateConnectionState(string message, bool connected)
    {
        _connectionStatus.Text = message;
        _connectionStatus.ForeColor = connected ? UiTheme.SuccessColor : UiTheme.MutedTextColor;
        ConnectionStateChanged?.Invoke(connected, message);
    }

    private void UpdateButtonState(bool running)
    {
        _startButton.Enabled = !running;
        _stopButton.Enabled = running;
        _hostText.Enabled = !running;
        _portBox.Enabled = !running;
    }

    private void AppendLog(string source, string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{source}] {message}";
        _logText.AppendText(line + Environment.NewLine);
        UiLogLimiter.Trim(_logText);
        LogGenerated?.Invoke("TCP结果监听", source, message);
    }

    private void PostToUi(Action action)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(action);
            return;
        }

        action();
    }
}

internal sealed class TcpMonitorService : IDisposable
{
    private static readonly int[] ReconnectDelaysSeconds = [1, 2, 5, 10, 30];
    private const int MaxPendingMessageCharacters = 65_536;
    private readonly TcpMonitorConfig _config;
    private readonly CancellationTokenSource _stop = new();
    private Task? _runTask;
    private TcpClient? _client;

    public event Action<string>? Logged;
    public event Action<string, bool>? StateChanged;
    public event Action<string, TcpMatchResult>? MessageReceived;

    public TcpMonitorService(TcpMonitorConfig config)
    {
        _config = config;
    }

    public void Start()
    {
        _runTask ??= Task.Run(() => RunAsync(_stop.Token));
    }

    public void Dispose()
    {
        _stop.Cancel();
        _client?.Dispose();
        var runTask = _runTask;
        if (runTask is null)
        {
            _stop.Dispose();
        }
        else
        {
            _ = runTask.ContinueWith(_ => _stop.Dispose(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var retryIndex = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                StateChanged?.Invoke(retryIndex == 0 ? "连接中" : "重连中", false);
                var client = new TcpClient { NoDelay = true };
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                _client = client;
                using var connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                connectTimeout.CancelAfter(TimeSpan.FromSeconds(10));
                try
                {
                    await client.ConnectAsync(_config.Host, _config.Port, connectTimeout.Token);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException("连接超时（10秒）。");
                }
                retryIndex = 0;
                StateChanged?.Invoke("已连接", true);
                Logged?.Invoke($"已连接 {_config.Host}:{_config.Port}");
                await ReceiveAsync(client, cancellationToken);
                Logged?.Invoke("远端已断开连接。");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Logged?.Invoke($"连接异常：{ex.Message}");
            }
            finally
            {
                _client?.Dispose();
                _client = null;
                StateChanged?.Invoke("未连接", false);
            }

            var delaySeconds = ReconnectDelaysSeconds[Math.Min(retryIndex, ReconnectDelaysSeconds.Length - 1)];
            retryIndex++;
            StateChanged?.Invoke($"等待重连（{delaySeconds}s）", false);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ReceiveAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var stream = client.GetStream();
        var bytes = new byte[8192];
        var pending = new StringBuilder();

        while (!cancellationToken.IsCancellationRequested)
        {
            using var idleTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            idleTimeout.CancelAfter(_config.PartialMessageTimeoutMs);
            int count;
            try
            {
                count = await stream.ReadAsync(bytes, idleTimeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (pending.Length > 0)
                {
                    Dispatch(pending.ToString());
                    pending.Clear();
                }
                continue;
            }

            if (count == 0)
            {
                return;
            }

            pending.Append(Encoding.UTF8.GetString(bytes, 0, count));
            DispatchCompleteLines(pending);
            if (pending.Length > MaxPendingMessageCharacters)
            {
                Logged?.Invoke($"无换行报文超过 {MaxPendingMessageCharacters} 字符，已按当前内容强制补帧。");
                Dispatch(pending.ToString());
                pending.Clear();
            }
        }
    }

    private void DispatchCompleteLines(StringBuilder pending)
    {
        while (true)
        {
            var text = pending.ToString();
            var newline = text.IndexOf('\n');
            if (newline < 0)
            {
                return;
            }

            Dispatch(text[..newline].TrimEnd('\r'));
            pending.Remove(0, newline + 1);
        }
    }

    private void Dispatch(string message)
    {
        var normalized = message.Trim();
        if (normalized.Length == 0)
        {
            return;
        }

        MessageReceived?.Invoke(normalized, TcpResultParser.Parse(normalized, _config));
    }
}

internal static class TcpResultParser
{
    private static readonly Regex OkToken = new(@"(?<![A-Z0-9_])OK(?![A-Z0-9_])", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex NgToken = new(@"(?<![A-Z0-9_])NG(?![A-Z0-9_])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static TcpMatchResult Parse(string message, TcpMonitorConfig config)
    {
        var candidate = config.ParseMode switch
        {
            TcpParseMode.DelimitedField => ReadField(message, config.Delimiter, config.FieldIndex),
            TcpParseMode.KeyValue => ReadKeyValue(message, config.ResultKey),
            _ => message
        };

        var hasNg = NgToken.IsMatch(candidate);
        var hasOk = OkToken.IsMatch(candidate);
        return hasNg ? TcpMatchResult.Ng : hasOk ? TcpMatchResult.Ok : TcpMatchResult.None;
    }

    private static string ReadField(string message, string delimiter, int oneBasedIndex)
    {
        if (string.IsNullOrEmpty(delimiter))
        {
            return string.Empty;
        }

        var fields = message.Split(delimiter, StringSplitOptions.TrimEntries);
        var index = oneBasedIndex - 1;
        return index >= 0 && index < fields.Length ? fields[index] : string.Empty;
    }

    private static string ReadKeyValue(string message, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        var pattern = $@"(?:^|[\s,;]){Regex.Escape(key)}\s*=\s*(?<value>[^\s,;]+)";
        var match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["value"].Value : string.Empty;
    }
}

internal static class ResultAudioPlayer
{
    private static readonly object SyncRoot = new();
    private static System.Threading.Timer? _stopTimer;
    private static string? _lastPath;
    private static DateTime _lastPlayUtc = DateTime.MinValue;
    private static readonly TimeSpan MinimumReplayInterval = TimeSpan.FromMilliseconds(500);
    private const uint SoundAsync = 0x0001;
    private const uint SoundFileName = 0x00020000;
    private const uint SoundNoDefault = 0x0002;
    private const uint SoundPurge = 0x0040;

    [DllImport("winmm.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool PlaySound(string? soundName, IntPtr module, uint flags);

    public static void Play(string path, int durationMs)
    {
        lock (SyncRoot)
        {
            var now = DateTime.UtcNow;
            if (string.Equals(path, _lastPath, StringComparison.OrdinalIgnoreCase) && now - _lastPlayUtc < MinimumReplayInterval)
            {
                return;
            }
            _lastPath = path;
            _lastPlayUtc = now;
            _stopTimer?.Dispose();
            PlaySound(path, IntPtr.Zero, SoundAsync | SoundFileName | SoundNoDefault);
            _stopTimer = new System.Threading.Timer(
                _ =>
                {
                    lock (SyncRoot)
                    {
                        PlaySound(null, IntPtr.Zero, SoundPurge);
                        _stopTimer?.Dispose();
                        _stopTimer = null;
                    }
                },
                null,
                Math.Clamp(durationMs, 100, 30000),
                Timeout.Infinite);
        }
    }
}

internal static class UiLogLimiter
{
    private const int MaxCharacters = 200_000;
    private const int TrimToCharacters = 150_000;

    public static void Trim(TextBox textBox)
    {
        if (textBox.TextLength <= MaxCharacters)
        {
            return;
        }

        textBox.Select(0, textBox.TextLength - TrimToCharacters);
        textBox.SelectedText = string.Empty;
    }
}

internal enum TcpParseMode
{
    WholeMessage,
    DelimitedField,
    KeyValue
}

internal enum TcpMatchResult
{
    None,
    Ok,
    Ng
}

internal sealed record TcpMonitorConfig(
    string Host,
    int Port,
    bool StartOnLaunch,
    TcpParseMode ParseMode,
    string Delimiter,
    int FieldIndex,
    string ResultKey,
    int PartialMessageTimeoutMs,
    int AudioPlaybackDurationMs = 1500)
{
    public static TcpMonitorConfig Default { get; } = new(
        Host: "127.0.0.1",
        Port: 9000,
        StartOnLaunch: true,
        ParseMode: TcpParseMode.WholeMessage,
        Delimiter: ",",
        FieldIndex: 2,
        ResultKey: "result",
        PartialMessageTimeoutMs: 150,
        AudioPlaybackDurationMs: 1500);
}
