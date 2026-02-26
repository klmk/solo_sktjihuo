using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HardwareHook.Core.Configuration;
using HardwareHook.Core.HardwareInfo;
using HardwareHook.Core.Logging;
using Newtonsoft.Json;

namespace HardwareHook.App
{
    public class MainForm : Form
    {
        private ILogger _logger;
        private readonly Dictionary<int, string> _uninstallEvents = new Dictionary<int, string>();

        private TabControl _tabControl;

        // 进程管理页
        private DataGridView _processGrid;
        private TextBox _filterProcessName;
        private Button _btnRefreshProcesses;
        private Button _btnSelectConfig;
        private Button _btnInject;
        private Button _btnUninstall;

        // 硬件信息页
        private DataGridView _hardwareGrid;
        private Button _btnReadHardware;
        private Button _btnExportConfig;
        private Button _btnEditConfig;
        private HardwareInfoSnapshot _lastHardwareSnapshot;

        // Hook 管理页
        private DataGridView _hooksProcessGrid;
        private Button _btnUninstallSelectedHook;
        private Button _btnUninstallAllHooks;
        private Button _btnVerifyHook;

        // 日志查看页
        private TextBox _logTextBox;
        private Button _btnLogClear;
        private Button _btnLogExport;
        private Button _btnLogRefresh;
        private readonly List<string> _logLines = new List<string>();
        private const int MaxLogLines = 2000;

        private string _currentConfigPath;

        public MainForm()
        {
            Text = "Windows 硬件信息模拟 Hook 工具";
            Width = 1000;
            Height = 700;

            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "HardwareHook",
                "Logs");
            var fileLogger = new FileLogger(logDir);

            InitializeComponents();
            _logger = new UiAwareLogger(fileLogger, this, AppendLogLine);
            _logger.Info("程序启动。", "App");
            LoadProcessList();
        }

        private void InitializeComponents()
        {
            _tabControl = new TabControl { Dock = DockStyle.Fill };

            var tpProcesses = new TabPage("进程管理");
            var tpHardware = new TabPage("硬件信息");
            var tpHooks = new TabPage("Hook管理");
            var tpLogs = new TabPage("日志查看");

            InitializeProcessTab(tpProcesses);
            InitializeHardwareTab(tpHardware);
            InitializeHooksTab(tpHooks);
            InitializeLogsTab(tpLogs);

            _tabControl.TabPages.Add(tpProcesses);
            _tabControl.TabPages.Add(tpHardware);
            _tabControl.TabPages.Add(tpHooks);
            _tabControl.TabPages.Add(tpLogs);

            _tabControl.SelectedIndexChanged += (s, ev) =>
            {
                if (_tabControl.SelectedTab?.Text == "日志查看")
                    BeginInvoke(new Action(FlushLogBufferToTextBox));
            };

            Controls.Add(_tabControl);
        }

        private void FlushLogBufferToTextBox()
        {
            if (_logTextBox == null || _logTextBox.IsDisposed) return;
            if (!_logTextBox.IsHandleCreated) return;
            lock (_logLines)
            {
                _logTextBox.Clear();
                foreach (var line in _logLines)
                    _logTextBox.AppendText(line + Environment.NewLine);
            }
        }

        private void AppendLogLine(string line)
        {
            if (line == null) return;
            lock (_logLines)
            {
                _logLines.Add(line);
                while (_logLines.Count > MaxLogLines) _logLines.RemoveAt(0);
            }
            if (_logTextBox != null && !_logTextBox.IsDisposed && _logTextBox.IsHandleCreated)
            {
                try
                {
                    if (_logTextBox.InvokeRequired)
                        _logTextBox.BeginInvoke(new Action(() => AppendLogToTextBox(line)));
                    else
                        AppendLogToTextBox(line);
                }
                catch { }
            }
        }

        private void AppendLogToTextBox(string line)
        {
            if (_logTextBox == null || _logTextBox.IsDisposed) return;
            _logTextBox.AppendText(line + Environment.NewLine);
        }

        private void InitializeProcessTab(TabPage page)
        {
            _processGrid = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 400,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };
            _processGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "进程名称", DataPropertyName = "ProcessName", Width = 250 });
            _processGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PID", DataPropertyName = "Id", Width = 80 });
            _processGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "状态", DataPropertyName = "Status", Width = 120 });

            var searchPanel = new Panel { Dock = DockStyle.Top, Height = 32, Padding = new Padding(0, 4, 0, 0) };
            var lblSearch = new Label { Text = "搜索进程名称或 PID：", AutoSize = true, Left = 10, Top = 8 };
            _filterProcessName = new TextBox
            {
                Left = 160,
                Top = 4,
                Width = 220
            };
            _filterProcessName.TextChanged += (_, __) => LoadProcessList();
            _filterProcessName.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadProcessList(); } };
            var btnSearch = new Button { Text = "搜索", Width = 70, Left = 390, Top = 4 };
            btnSearch.Click += (_, __) => LoadProcessList();
            searchPanel.Controls.Add(btnSearch);
            searchPanel.Controls.Add(_filterProcessName);
            searchPanel.Controls.Add(lblSearch);

            _btnRefreshProcesses = new Button { Text = "刷新进程列表", Width = 120, Left = 10, Top = 430 };
            _btnRefreshProcesses.Click += (_, __) => LoadProcessList();
            _btnSelectConfig = new Button { Text = "选择配置文件", Width = 120, Left = 150, Top = 430 };
            _btnSelectConfig.Click += (_, __) => SelectConfigFile();
            _btnInject = new Button { Text = "注入Hook", Width = 120, Left = 290, Top = 430 };
            _btnInject.Click += (_, __) => InjectSelectedProcess();
            _btnUninstall = new Button { Text = "停止Hook", Width = 120, Left = 430, Top = 430 };
            _btnUninstall.Click += (_, __) => UninstallSelectedProcess();

            page.Controls.Add(_btnUninstall);
            page.Controls.Add(_btnInject);
            page.Controls.Add(_btnSelectConfig);
            page.Controls.Add(_btnRefreshProcesses);
            page.Controls.Add(searchPanel);
            page.Controls.Add(_processGrid);
        }

        private void InitializeHardwareTab(TabPage page)
        {
            _hardwareGrid = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 380,
                ReadOnly = true,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };
            _hardwareGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "属性", Width = 180 });
            _hardwareGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "值", Width = 400 });

            _btnReadHardware = new Button { Text = "读取当前硬件信息", Width = 140, Left = 10, Top = 400 };
            _btnReadHardware.Click += (_, __) => ReadHardwareInfo();
            _btnExportConfig = new Button { Text = "导出配置模板", Width = 120, Left = 170, Top = 400 };
            _btnExportConfig.Click += (_, __) => ExportConfigTemplate();
            _btnEditConfig = new Button { Text = "编辑配置", Width = 100, Left = 310, Top = 400 };
            _btnEditConfig.Click += (_, __) => EditConfig();

            page.Controls.Add(_btnEditConfig);
            page.Controls.Add(_btnExportConfig);
            page.Controls.Add(_btnReadHardware);
            page.Controls.Add(_hardwareGrid);
        }

        private void InitializeHooksTab(TabPage page)
        {
            _hooksProcessGrid = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 280,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };
            _hooksProcessGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "进程名称", DataPropertyName = "ProcessName", Width = 220 });
            _hooksProcessGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PID", DataPropertyName = "Id", Width = 80 });
            _hooksProcessGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "状态", DataPropertyName = "Status", Width = 100 });

            var lblStatus = new Label
            {
                Text = "Hook 状态：CPU信息Hook / 硬盘信息Hook / 网络信息Hook / 主板信息Hook（注入后在该进程内生效）",
                AutoSize = true,
                Left = 10,
                Top = 290,
                MaximumSize = new System.Drawing.Size(700, 0)
            };

            _btnUninstallSelectedHook = new Button { Text = "卸载选中进程Hook", Width = 140, Left = 10, Top = 330 };
            _btnUninstallSelectedHook.Click += (_, __) => UninstallSelectedHookFromHooksTab();
            _btnUninstallAllHooks = new Button { Text = "全部卸载", Width = 100, Left = 170, Top = 330 };
            _btnUninstallAllHooks.Click += (_, __) => UninstallAllHooks();
            _btnVerifyHook = new Button { Text = "验证Hook效果", Width = 120, Left = 290, Top = 330 };
            _btnVerifyHook.Click += (_, __) => VerifyHookEffect();

            page.Controls.Add(_btnVerifyHook);
            page.Controls.Add(_btnUninstallAllHooks);
            page.Controls.Add(_btnUninstallSelectedHook);
            page.Controls.Add(lblStatus);
            page.Controls.Add(_hooksProcessGrid);
        }

        private void InitializeLogsTab(TabPage page)
        {
            _logTextBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Consolas", 9f)
            };

            var panel = new Panel { Dock = DockStyle.Bottom, Height = 40 };
            _btnLogClear = new Button { Text = "清空日志", Width = 90, Left = 10, Top = 8 };
            _btnLogClear.Click += (_, __) => { _logTextBox.Clear(); lock (_logLines) _logLines.Clear(); };
            _btnLogExport = new Button { Text = "导出日志", Width = 90, Left = 110, Top = 8 };
            _btnLogExport.Click += (_, __) => ExportLog();
            _btnLogRefresh = new Button { Text = "刷新日志", Width = 90, Left = 210, Top = 8 };
            _btnLogRefresh.Click += (_, __) => RefreshLogFromBuffer();

            panel.Controls.Add(_btnLogRefresh);
            panel.Controls.Add(_btnLogExport);
            panel.Controls.Add(_btnLogClear);
            page.Controls.Add(_logTextBox);
            page.Controls.Add(panel);
        }

        private void ReadHardwareInfo()
        {
            try
            {
                _lastHardwareSnapshot = HardwareInfoReader.GetCurrent(_logger);
                var rows = new List<object[]>
                {
                    new object[] { "处理器型号", _lastHardwareSnapshot.CpuModel },
                    new object[] { "核心数", _lastHardwareSnapshot.CpuCoreCount.ToString() },
                    new object[] { "CpuId", _lastHardwareSnapshot.CpuId },
                    new object[] { "硬盘序列号", _lastHardwareSnapshot.DiskSerial },
                    new object[] { "MAC 地址", _lastHardwareSnapshot.MacAddress },
                    new object[] { "主板序列号", _lastHardwareSnapshot.MotherboardSerial }
                };
                _hardwareGrid.Rows.Clear();
                foreach (var r in rows)
                    _hardwareGrid.Rows.Add(r);
                _logger.Info("已读取当前硬件信息。", "Hardware");
                MessageBox.Show(this, "硬件信息已读取并显示在列表中。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.Error("读取硬件信息失败。", ex, "Hardware");
                MessageBox.Show(this, "读取失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportConfigTemplate()
        {
            var snapshot = _lastHardwareSnapshot;
            if (snapshot == null)
            {
                MessageBox.Show(this, "请先点击「读取当前硬件信息」。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "JSON 配置文件 (*.json)|*.json|所有文件 (*.*)|*.*";
                dlg.Title = "导出配置模板";
                dlg.FileName = "hardware_config.json";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    var config = new HardwareConfig
                    {
                        Version = "1.0",
                        Cpu = new CpuConfig { Model = snapshot.CpuModel, CoreCount = snapshot.CpuCoreCount, CpuId = snapshot.CpuId },
                        Disk = new DiskConfig { Serial = snapshot.DiskSerial },
                        Mac = new MacConfig { Address = snapshot.MacAddress },
                        Motherboard = new MotherboardConfig { Serial = snapshot.MotherboardSerial }
                    };
                    File.WriteAllText(dlg.FileName, JsonConvert.SerializeObject(config, Formatting.Indented), Encoding.UTF8);
                    _logger.Info($"已导出配置模板：{dlg.FileName}", "Hardware");
                    MessageBox.Show(this, "配置模板已导出。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    _logger.Error("导出配置失败。", ex, "Hardware");
                    MessageBox.Show(this, "导出失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void EditConfig()
        {
            if (string.IsNullOrEmpty(_currentConfigPath))
            {
                MessageBox.Show(this, "请先在「进程管理」中选择一个配置文件，或先导出配置模板后再选择该文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo(_currentConfigPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "无法打开配置文件：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshHooksTabGrid()
        {
            var list = new List<HookProcessRow>();
            foreach (var kv in _uninstallEvents)
            {
                try
                {
                    var proc = Process.GetProcessById(kv.Key);
                    if (!proc.HasExited)
                        list.Add(new HookProcessRow { ProcessName = proc.ProcessName, Id = kv.Key, Status = "模拟中" });
                }
                catch { /* 进程已退出 */ }
            }
            _hooksProcessGrid.DataSource = list;
        }

        private sealed class HookProcessRow
        {
            public string ProcessName { get; set; }
            public int Id { get; set; }
            public string Status { get; set; }
        }

        private void UninstallSelectedHookFromHooksTab()
        {
            if (_hooksProcessGrid.CurrentRow == null)
            {
                MessageBox.Show(this, "请先选择要卸载的进程。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var pidStr = _hooksProcessGrid.CurrentRow.Cells[1].Value?.ToString();
            if (!int.TryParse(pidStr, out var pid) || !_uninstallEvents.TryGetValue(pid, out var evt))
            {
                MessageBox.Show(this, "无法获取选中进程的卸载信息。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (InjectionHelper.TryUninstall(evt, _logger, out var err))
            {
                _uninstallEvents.Remove(pid);
                RefreshHooksTabGrid();
                LoadProcessList();
                MessageBox.Show(this, "卸载请求已发送。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
                MessageBox.Show(this, "卸载失败：" + err, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void UninstallAllHooks()
        {
            if (_uninstallEvents.Count == 0)
            {
                MessageBox.Show(this, "当前没有已注入的进程。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var list = new List<int>(_uninstallEvents.Keys);
            foreach (var pid in list)
            {
                if (_uninstallEvents.TryGetValue(pid, out var evt))
                    InjectionHelper.TryUninstall(evt, _logger, out _);
                _uninstallEvents.Remove(pid);
            }
            RefreshHooksTabGrid();
            LoadProcessList();
            MessageBox.Show(this, "已向所有已注入进程发送卸载请求。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void VerifyHookEffect()
        {
            var testerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HardwareHook.Tester.exe");
            if (!File.Exists(testerPath))
            {
                MessageBox.Show(this, "未找到 HardwareHook.Tester.exe，请将其与主程序放在同一目录。运行测试程序可查看当前进程内可见的硬件信息（若已注入则显示模拟值）。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo(testerPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "无法启动测试程序：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportLog()
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*";
                dlg.FileName = "hardwarehook_log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    string text;
                    lock (_logLines) text = string.Join(Environment.NewLine, _logLines);
                    File.WriteAllText(dlg.FileName, text, Encoding.UTF8);
                    MessageBox.Show(this, "日志已导出。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "导出失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void RefreshLogFromBuffer()
        {
            _logTextBox.Clear();
            lock (_logLines)
            {
                foreach (var line in _logLines)
                    _logTextBox.AppendText(line + Environment.NewLine);
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RefreshHooksTabGrid();
            lock (_logLines)
            {
                if (_logTextBox != null && _logTextBox.IsHandleCreated)
                {
                    foreach (var line in _logLines)
                        _logTextBox.AppendText(line + Environment.NewLine);
                }
            }
        }

        private void LoadProcessList()
        {
            try
            {
                var keyword = _filterProcessName?.Text?.Trim();
                var processes = Process.GetProcesses()
                    .OrderBy(p => p.ProcessName)
                    .Select(p => new { p.ProcessName, p.Id, Status = _uninstallEvents.ContainsKey(p.Id) ? "模拟中" : "未模拟" });
                if (!string.IsNullOrEmpty(keyword))
                {
                    processes = processes.Where(p =>
                        p.ProcessName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        p.Id.ToString().IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
                }
                _processGrid.DataSource = processes.ToList();
            }
            catch (Exception ex)
            {
                _logger?.Error("加载进程列表失败。", ex, "UI");
                MessageBox.Show(this, "加载进程列表失败，请查看日志。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SelectConfigFile()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "JSON 配置文件 (*.json)|*.json|所有文件 (*.*)|*.*";
                dialog.Title = "选择硬件模拟配置文件";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _currentConfigPath = dialog.FileName;
                    _logger?.Info($"已选择配置文件：{_currentConfigPath}", "UI");
                    Text = "Windows 硬件信息模拟 Hook 工具 - " + Path.GetFileName(_currentConfigPath);
                    var result = ConfigurationLoader.Load(_currentConfigPath, _logger);
                    if (!result.Success)
                    {
                        var errors = string.Join(Environment.NewLine, result.ValidationErrors);
                        MessageBox.Show(this, "配置文件验证失败：" + Environment.NewLine + errors, "配置错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                        MessageBox.Show(this, "配置文件验证通过。", "配置验证", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private bool TryGetSelectedProcessId(out int processId)
        {
            processId = 0;
            if (_processGrid.CurrentRow == null)
            {
                MessageBox.Show(this, "请先选择一个进程。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (!int.TryParse(_processGrid.CurrentRow.Cells[1].Value?.ToString(), out processId))
            {
                MessageBox.Show(this, "无法解析选中行的 PID。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        private void InjectSelectedProcess()
        {
            if (_currentConfigPath == null)
            {
                MessageBox.Show(this, "请先选择配置文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!TryGetSelectedProcessId(out var pid)) return;
            try
            {
                var proc = Process.GetProcessById(pid);
                if (proc.HasExited) { MessageBox.Show(this, "目标进程已退出。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            }
            catch
            {
                MessageBox.Show(this, "无法访问目标进程，可能已退出。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (InjectionHelper.TryInject(pid, _currentConfigPath, _logger, out var uninstallEvent, out var error))
            {
                _uninstallEvents[pid] = uninstallEvent;
                LoadProcessList();
                RefreshHooksTabGrid();
                MessageBox.Show(this, "注入成功。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
                MessageBox.Show(this, "注入失败：" + error, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void UninstallSelectedProcess()
        {
            if (!TryGetSelectedProcessId(out var pid)) return;
            if (!_uninstallEvents.TryGetValue(pid, out var uninstallEvent))
            {
                MessageBox.Show(this, "该进程当前未记录模拟状态。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (InjectionHelper.TryUninstall(uninstallEvent, _logger, out var error))
            {
                _uninstallEvents.Remove(pid);
                LoadProcessList();
                RefreshHooksTabGrid();
                MessageBox.Show(this, "卸载请求已发送。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
                MessageBox.Show(this, "卸载失败：" + error, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
