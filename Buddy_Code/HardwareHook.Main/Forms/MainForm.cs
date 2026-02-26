using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using HardwareHook.Core.Config;
using HardwareHook.Core.Logging;

namespace HardwareHook.Main.Forms
{
    public partial class MainForm : Form
    {
        private ProcessManager _processManager;
        private InjectionController _injectionController;
        private string _currentConfigPath;
        
        public MainForm()
        {
            InitializeComponent();
            InitializeLogging();
            InitializeControllers();
            LoadInitialData();
        }
        
        private void InitializeLogging()
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Logger.Initialize(logDir);
            Logger.Info("主程序启动", "MainForm");
        }
        
        private void InitializeControllers()
        {
            _processManager = new ProcessManager();
            _injectionController = new InjectionController();
            
            // 订阅事件
            _processManager.ProcessListUpdated += OnProcessListUpdated;
            _injectionController.InjectionStatusChanged += OnInjectionStatusChanged;
        }
        
        private void LoadInitialData()
        {
            try
            {
                // 加载进程列表
                btnRefreshProcesses_Click(this, EventArgs.Empty);
                
                // 加载默认配置
                LoadDefaultConfig();
                
                Logger.Info("初始数据加载完成", "MainForm");
            }
            catch (Exception ex)
            {
                Logger.Error($"初始数据加载失败: {ex.Message}", "MainForm");
                MessageBox.Show($"初始数据加载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void LoadDefaultConfig()
        {
            string defaultConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "default_config.json");
            
            if (File.Exists(defaultConfigPath))
            {
                _currentConfigPath = defaultConfigPath;
                lblConfigFile.Text = $"配置文件: {Path.GetFileName(_currentConfigPath)}";
                Logger.Info($"默认配置加载: {Path.GetFileName(defaultConfigPath)}", "MainForm");
            }
            else
            {
                // 创建默认配置
                CreateDefaultConfig(defaultConfigPath);
            }
        }
        
        private void CreateDefaultConfig(string path)
        {
            try
            {
                var config = new HardwareConfig
                {
                    Version = "1.0",
                    Cpu = new CpuConfig(),
                    Disk = new DiskConfig(),
                    Mac = new MacConfig(),
                    Motherboard = new MotherboardConfig()
                };
                
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(path, json);
                
                _currentConfigPath = path;
                lblConfigFile.Text = $"配置文件: {Path.GetFileName(_currentConfigPath)}";
                
                Logger.Info("默认配置文件已创建", "MainForm");
            }
            catch (Exception ex)
            {
                Logger.Error($"创建默认配置失败: {ex.Message}", "MainForm");
            }
        }
        
        private void OnProcessListUpdated(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnProcessListUpdated(sender, e)));
                return;
            }
            
            try
            {
                listViewProcesses.Items.Clear();
                var processes = _processManager.GetProcesses();
                
                foreach (var process in processes)
                {
                    var item = new ListViewItem(process.ProcessName);
                    item.SubItems.Add(process.Id.ToString());
                    item.SubItems.Add(process.Status);
                    item.SubItems.Add(_injectionController.IsInjected(process.Id) ? "模拟中" : "未模拟");
                    item.Tag = process;
                    
                    listViewProcesses.Items.Add(item);
                }
                
                lblStatus.Text = $"进程列表已刷新，共 {processes.Count} 个进程";
            }
            catch (Exception ex)
            {
                Logger.Error($"更新进程列表失败: {ex.Message}", "MainForm");
            }
        }
        
        private void OnInjectionStatusChanged(object sender, InjectionStatusEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnInjectionStatusChanged(sender, e)));
                return;
            }
            
            string message = e.Success ? 
                $"进程 {e.ProcessId} 注入成功" : 
                $"进程 {e.ProcessId} 注入失败: {e.Message}";
            
            AddLogEntry(e.Success ? LogLevel.Info : LogLevel.Error, message);
            
            if (!e.Success)
            {
                MessageBox.Show(e.Message, "注入失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            // 刷新进程列表
            btnRefreshProcesses_Click(this, EventArgs.Empty);
        }
        
        private void AddLogEntry(LogLevel level, string message)
        {
            try
            {
                var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
                item.SubItems.Add(level.ToString());
                item.SubItems.Add(message);
                
                if (level == LogLevel.Error)
                    item.ForeColor = System.Drawing.Color.Red;
                else if (level == LogLevel.Warning)
                    item.ForeColor = System.Drawing.Color.Orange;
                
                listViewLog.Items.Add(item);
                listViewLog.EnsureVisible(listViewLog.Items.Count - 1);
                
                // 限制日志条目数
                if (listViewLog.Items.Count > 1000)
                {
                    listViewLog.Items.RemoveAt(0);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"添加日志失败: {ex.Message}");
            }
        }
        
        private void btnRefreshProcesses_Click(object sender, EventArgs e)
        {
            try
            {
                btnRefreshProcesses.Enabled = false;
                lblStatus.Text = "正在刷新进程列表...";
                
                _processManager.RefreshProcesses();
                
                lblStatus.Text = "进程列表刷新完成";
            }
            catch (Exception ex)
            {
                Logger.Error($"刷新进程列表失败: {ex.Message}", "MainForm");
                MessageBox.Show($"刷新进程列表失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRefreshProcesses.Enabled = true;
            }
        }
        
        private void btnSelectConfig_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Filter = "JSON配置文件|*.json|所有文件|*.*";
                    dialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        _currentConfigPath = dialog.FileName;
                        lblConfigFile.Text = $"配置文件: {Path.GetFileName(_currentConfigPath)}";
                        
                        // 验证配置文件
                        try
                        {
                            var config = ConfigManager.Load(_currentConfigPath);
                            AddLogEntry(LogLevel.Info, $"配置文件已加载: {Path.GetFileName(_currentConfigPath)}");
                        }
                        catch (Exception ex)
                        {
                            AddLogEntry(LogLevel.Error, $"配置文件验证失败: {ex.Message}");
                            MessageBox.Show($"配置文件验证失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"选择配置文件失败: {ex.Message}", "MainForm");
                MessageBox.Show($"选择配置文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void btnInject_Click(object sender, EventArgs e)
        {
            try
                       {
                if (listViewProcesses.SelectedItems.Count == 0)
                {
                    MessageBox.Show("请先选择要注入的进程", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                if (string.IsNullOrEmpty(_currentConfigPath) || !File.Exists(_currentConfigPath))
                {
                    MessageBox.Show("请先选择有效的配置文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                var selectedProcess = listViewProcesses.SelectedItems[0].Tag as ProcessInfo;
                if (selectedProcess == null)
                {
                    MessageBox.Show("无效的进程信息", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                // 检查是否已注入
                if (_injectionController.IsInjected(selectedProcess.Id))
                {
                    MessageBox.Show("该进程已经注入，请勿重复操作", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                btnInject.Enabled = false;
                lblStatus.Text = $"正在注入进程 {selectedProcess.ProcessName}...";
                
                // 执行注入
                var result = _injectionController.Inject(selectedProcess.Id, _currentConfigPath);
                
                if (result.Success)
                {
                    AddLogEntry(LogLevel.Info, $"进程 {selectedProcess.ProcessName}({selectedProcess.Id}) 注入成功");
                    lblStatus.Text = "注入成功";
                }
                else
                {
                    AddLogEntry(LogLevel.Error, $"进程 {selectedProcess.ProcessName}({selectedProcess.Id}) 注入失败: {result.Message}");
                    lblStatus.Text = "注入失败";
                    MessageBox.Show(result.Message, "注入失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"注入失败: {ex.Message}", "MainForm");
                AddLogEntry(LogLevel.Error, $"注入失败: {ex.Message}");
                MessageBox.Show($"注入失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnInject.Enabled = true;
                btnRefreshProcesses_Click(this, EventArgs.Empty);
            }
        }
        
        private void btnStopSimulation_Click(object sender, EventArgs e)
        {
            try
            {
                if (listViewProcesses.SelectedItems.Count == 0)
                {
                    MessageBox.Show("请先选择要停止的进程", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                var selectedProcess = listViewProcesses.SelectedItems[0].Tag as ProcessInfo;
                if (selectedProcess == null)
                {
                    MessageBox.Show("无效的进程信息", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                if (!_injectionController.IsInjected(selectedProcess.Id))
                {
                    MessageBox.Show("该进程未注入", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                btnStopSimulation.Enabled = false;
                lblStatus.Text = $"正在停止进程 {selectedProcess.ProcessName}...";
                
                _injectionController.StopSimulation(selectedProcess.Id);
                
                AddLogEntry(LogLevel.Info, $"进程 {selectedProcess.ProcessName}({selectedProcess.Id}) 已停止模拟");
                lblStatus.Text = "停止成功";
            }
            catch (Exception ex)
            {
                Logger.Error($"停止失败: {ex.Message}", "MainForm");
                AddLogEntry(LogLevel.Error, $"停止失败: {ex.Message}");
                MessageBox.Show($"停止失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnStopSimulation.Enabled = true;
                btnRefreshProcesses_Click(this, EventArgs.Empty);
            }
        }
        
        private void btnExportConfig_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dialog = new SaveFileDialog())
                {
                    dialog.Filter = "JSON配置文件|*.json|所有文件|*.*";
                    dialog.FileName = "hardware_config.json";
                    dialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        CreateDefaultConfig(dialog.FileName);
                        AddLogEntry(LogLevel.Info, $"配置文件已导出: {Path.GetFileName(dialog.FileName)}");
                        MessageBox.Show("配置文件导出成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"导出配置失败: {ex.Message}", "MainForm");
                MessageBox.Show($"导出配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void btnClearLog_Click(object sender, EventArgs e)
        {
            listViewLog.Items.Clear();
        }
        
        private void listViewProcesses_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // 简单的列点击排序
            try
            {
                var sorter = new ListViewColumnSorter();
                sorter.ColumnIndex = e.Column;
                sorter.SortOrder = SortOrder.Ascending;
                listViewProcesses.ListViewItemSorter = sorter;
                listViewProcesses.Sort();
            }
            catch { }
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                Logger.Info("主程序关闭", "MainForm");
                
                // 停止所有模拟
                _injectionController?.StopAllSimulations();
                
                base.OnFormClosing(e);
            }
            catch { }
        }
        
        // 日志级别枚举
        private enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error
        }
    }
    
    // 简单的列排序器
    public class ListViewColumnSorter : System.Collections.IComparer
    {
        public int ColumnIndex { get; set; }
        public SortOrder SortOrder { get; set; }
        
        public int Compare(object x, object y)
        {
            var itemX = x as ListViewItem;
            var itemY = y as ListViewItem;
            
            if (itemX == null || itemY == null)
                return 0;
            
            string textX = itemX.SubItems[ColumnIndex].Text;
            string textY = itemY.SubItems[ColumnIndex].Text;
            
            // 尝试数字比较
            if (int.TryParse(textX, out int numX) && int.TryParse(textY, out int numY))
            {
                return SortOrder == SortOrder.Ascending ? numX.CompareTo(numY) : numY.CompareTo(numX);
            }
            
            // 字符串比较
            return SortOrder == SortOrder.Ascending ? 
                string.Compare(textX, textY) : 
                string.Compare(textY, textX);
        }
    }
}
