using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using EasyHook;
using HardwareHook.Core.Configuration;
using HardwareHook.Core.HardwareInfo;
using HardwareHook.Core.Logging;

namespace HardwareHook.App
{
    /// <summary>
    /// 主窗体
    /// </summary>
    public partial class MainForm : Form
    {
        private ILogger _logger;
        private string _configPath;
        private string _coreDllPath;
        private string _configDirectory;
        private System.Threading.Timer _processRefreshTimer;

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            InitializeApp();
        }

        /// <summary>
        /// 初始化应用程序
        /// </summary>
        private void InitializeApp()
        {
            // 初始化日志
            _logger = new FileLogger();
            _logger.Info("Application started");

            // 设置配置文件目录
            _configDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs");
            Directory.CreateDirectory(_configDirectory);

            // 设置默认配置文件路径
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

            // 设置核心DLL路径
            _coreDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HardwareHook.Core.dll");

            // 加载进程列表
            LoadProcesses();

            // 加载配置文件列表
            LoadConfigFiles();

            // 初始化定时器，定期刷新进程列表
            _processRefreshTimer = new System.Threading.Timer(
                (state) => Invoke(new Action(LoadProcesses)),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5));

            // 检查配置文件
            CheckConfigFile();

            // 检查核心DLL
            CheckCoreDll();
        }

        /// <summary>
        /// 检查配置文件
        /// </summary>
        private void CheckConfigFile()
        {
            if (!File.Exists(_configPath))
            {
                // 创建默认配置文件
                CreateDefaultConfig();
            }
        }

        /// <summary>
        /// 创建默认配置文件
        /// </summary>
        private void CreateDefaultConfig()
        {
            try
            {
                var defaultConfig = new HardwareConfig();
                ConfigurationLoader.SaveConfiguration(defaultConfig, _configPath);
                _logger.Info("Default configuration created");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to create default configuration", ex);
                MessageBox.Show("创建默认配置文件失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 检查核心DLL
        /// </summary>
        private void CheckCoreDll()
        {
            if (!File.Exists(_coreDllPath))
            {
                _logger.Error("HardwareHook.Core.dll not found");
                MessageBox.Show("找不到核心DLL文件: HardwareHook.Core.dll", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnInject.Enabled = false;
            }
        }

        /// <summary>
        /// 加载进程列表
        /// </summary>
        private void LoadProcesses()
        {
            try
            {
                var processes = Process.GetProcesses();
                lstProcesses.Items.Clear();

                foreach (var process in processes)
                {
                    try
                    {
                        lstProcesses.Items.Add(new ProcessItem
                        {
                            ProcessId = process.Id,
                            ProcessName = process.ProcessName,
                            WindowTitle = process.MainWindowTitle
                        });
                    }
                    catch
                    {
                        // 跳过无法访问的进程
                    }
                }

                _logger.Info($"Loaded {lstProcesses.Items.Count} processes");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to load processes", ex);
                MessageBox.Show("加载进程列表失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 读取硬件信息
        /// </summary>
        private void ReadHardwareInfo()
        {
            try
            {
                var snapshot = HardwareInfoReader.ReadHardwareInfo();
                txtHardwareInfo.Text = snapshot.ToString();
                _logger.Info("Hardware info read successfully");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to read hardware info", ex);
                MessageBox.Show("读取硬件信息失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 导出配置
        /// </summary>
        private void ExportConfig()
        {
            try
            {
                var snapshot = HardwareInfoReader.ReadHardwareInfo();
                if (!snapshot.IsSuccess)
                {
                    MessageBox.Show("读取硬件信息失败，无法导出配置", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var config = new HardwareConfig
                {
                    Cpu = new CpuConfig
                    {
                        Model = snapshot.CpuModel,
                        CoreCount = snapshot.CpuCoreCount,
                        CpuId = snapshot.CpuId
                    },
                    Disk = new DiskConfig
                    {
                        Serial = snapshot.DiskSerial
                    },
                    Mac = new MacConfig
                    {
                        Address = snapshot.MacAddress
                    },
                    Motherboard = new MotherboardConfig
                    {
                        Serial = snapshot.MotherboardSerial
                    }
                };

                if (ConfigurationLoader.SaveConfiguration(config, _configPath))
                {
                    _logger.Info("Configuration exported successfully");
                    MessageBox.Show("配置导出成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    _logger.Error("Failed to export configuration");
                    MessageBox.Show("配置导出失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to export configuration", ex);
                MessageBox.Show("配置导出失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 注入DLL
        /// </summary>
        private void InjectDll()
        {
            if (lstProcesses.SelectedItem == null)
            {
                MessageBox.Show("请选择一个进程", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var selectedProcess = (ProcessItem)lstProcesses.SelectedItem;
                int processId = selectedProcess.ProcessId;

                // 生成唯一的卸载事件名称
                string uninstallEventName = $"HardwareHook_Uninstall_{Guid.NewGuid()}";

                // 注入参数
                object[] injectionArgs = { uninstallEventName, _configPath };

                // 执行注入
                RemoteHooking.Inject(
                    processId,
                    _coreDllPath,
                    _coreDllPath,
                    injectionArgs);

                _logger.Info($"Injected into process {processId} ({selectedProcess.ProcessName})");
                MessageBox.Show($"注入成功: {selectedProcess.ProcessName}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 记录注入状态
                selectedProcess.IsInjected = true;
                lstProcesses.Refresh();
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to inject DLL", ex);
                MessageBox.Show("注入失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 读取日志
        /// </summary>
        private void ReadLogs()
        {
            try
            {
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(logDirectory))
                {
                    MessageBox.Show("日志目录不存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 获取最新的日志文件
                var logFiles = Directory.GetFiles(logDirectory, "*.log");
                if (logFiles.Length == 0)
                {
                    MessageBox.Show("没有日志文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Array.Sort(logFiles, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
                string latestLogFile = logFiles[0];

                // 读取日志内容
                string logContent = File.ReadAllText(latestLogFile);
                txtLog.Text = logContent;
                _logger.Info("Logs read successfully");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to read logs", ex);
                MessageBox.Show("读取日志失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tabControl1 = new System.Windows.Forms.TabControl();
                this.tabPage1 = new System.Windows.Forms.TabPage();
                this.lstProcesses = new System.Windows.Forms.ListBox();
                this.btnInject = new System.Windows.Forms.Button();
                this.label1 = new System.Windows.Forms.Label();
                this.btnRefresh = new System.Windows.Forms.Button();
                this.tabPage2 = new System.Windows.Forms.TabPage();
                this.txtHardwareInfo = new System.Windows.Forms.TextBox();
                this.btnReadHardware = new System.Windows.Forms.Button();
                this.btnExportConfig = new System.Windows.Forms.Button();
                this.label2 = new System.Windows.Forms.Label();
                this.tabPage3 = new System.Windows.Forms.TabPage();
                this.txtLog = new System.Windows.Forms.TextBox();
                this.btnReadLog = new System.Windows.Forms.Button();
                this.label3 = new System.Windows.Forms.Label();
                this.tabPage4 = new System.Windows.Forms.TabPage();
                this.lstConfigFiles = new System.Windows.Forms.ListBox();
                this.btnLoadConfig = new System.Windows.Forms.Button();
                this.btnCreateConfig = new System.Windows.Forms.Button();
                this.btnDeleteConfig = new System.Windows.Forms.Button();
                this.label4 = new System.Windows.Forms.Label();
                this.btnRefreshConfig = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(776, 436);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.btnRefresh);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.btnInject);
            this.tabPage1.Controls.Add(this.lstProcesses);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(768, 410);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "进程管理";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // lstProcesses
            // 
            this.lstProcesses.FormattingEnabled = true;
            this.lstProcesses.ItemHeight = 12;
            this.lstProcesses.Location = new System.Drawing.Point(6, 25);
            this.lstProcesses.Name = "lstProcesses";
            this.lstProcesses.Size = new System.Drawing.Size(756, 340);
            this.lstProcesses.TabIndex = 0;
            // 
            // btnInject
            // 
            this.btnInject.Location = new System.Drawing.Point(6, 371);
            this.btnInject.Name = "btnInject";
            this.btnInject.Size = new System.Drawing.Size(75, 23);
            this.btnInject.TabIndex = 1;
            this.btnInject.Text = "注入";
            this.btnInject.UseVisualStyleBackColor = true;
            this.btnInject.Click += new System.EventHandler(this.btnInject_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 2;
            this.label1.Text = "进程列表";
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(87, 371);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(75, 23);
            this.btnRefresh.TabIndex = 3;
            this.btnRefresh.Text = "刷新";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.btnExportConfig);
            this.tabPage2.Controls.Add(this.btnReadHardware);
            this.tabPage2.Controls.Add(this.txtHardwareInfo);
            this.tabPage2.Controls.Add(this.label2);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(768, 410);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "硬件信息";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // txtHardwareInfo
            // 
            this.txtHardwareInfo.Location = new System.Drawing.Point(6, 25);
            this.txtHardwareInfo.Multiline = true;
            this.txtHardwareInfo.Name = "txtHardwareInfo";
            this.txtHardwareInfo.ReadOnly = true;
            this.txtHardwareInfo.Size = new System.Drawing.Size(756, 340);
            this.txtHardwareInfo.TabIndex = 0;
            // 
            // btnReadHardware
            // 
            this.btnReadHardware.Location = new System.Drawing.Point(6, 371);
            this.btnReadHardware.Name = "btnReadHardware";
            this.btnReadHardware.Size = new System.Drawing.Size(75, 23);
            this.btnReadHardware.TabIndex = 1;
            this.btnReadHardware.Text = "读取硬件";
            this.btnReadHardware.UseVisualStyleBackColor = true;
            this.btnReadHardware.Click += new System.EventHandler(this.btnReadHardware_Click);
            // 
            // btnExportConfig
            // 
            this.btnExportConfig.Location = new System.Drawing.Point(87, 371);
            this.btnExportConfig.Name = "btnExportConfig";
            this.btnExportConfig.Size = new System.Drawing.Size(75, 23);
            this.btnExportConfig.TabIndex = 2;
            this.btnExportConfig.Text = "导出配置";
            this.btnExportConfig.UseVisualStyleBackColor = true;
            this.btnExportConfig.Click += new System.EventHandler(this.btnExportConfig_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 12);
            this.label2.TabIndex = 3;
            this.label2.Text = "硬件信息";
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.btnReadLog);
            this.tabPage3.Controls.Add(this.txtLog);
            this.tabPage3.Controls.Add(this.label3);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(768, 410);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "日志查看";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(6, 25);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtLog.Size = new System.Drawing.Size(756, 340);
            this.txtLog.TabIndex = 0;
            // 
            // btnReadLog
            // 
            this.btnReadLog.Location = new System.Drawing.Point(6, 371);
            this.btnReadLog.Name = "btnReadLog";
            this.btnReadLog.Size = new System.Drawing.Size(75, 23);
            this.btnReadLog.TabIndex = 1;
            this.btnReadLog.Text = "读取日志";
            this.btnReadLog.UseVisualStyleBackColor = true;
            this.btnReadLog.Click += new System.EventHandler(this.btnReadLog_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(29, 12);
            this.label3.TabIndex = 2;
            this.label3.Text = "日志";
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.btnRefreshConfig);
            this.tabPage4.Controls.Add(this.btnDeleteConfig);
            this.tabPage4.Controls.Add(this.btnCreateConfig);
            this.tabPage4.Controls.Add(this.btnLoadConfig);
            this.tabPage4.Controls.Add(this.lstConfigFiles);
            this.tabPage4.Controls.Add(this.label4);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(768, 410);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "配置管理";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // lstConfigFiles
            // 
            this.lstConfigFiles.FormattingEnabled = true;
            this.lstConfigFiles.ItemHeight = 12;
            this.lstConfigFiles.Location = new System.Drawing.Point(6, 25);
            this.lstConfigFiles.Name = "lstConfigFiles";
            this.lstConfigFiles.Size = new System.Drawing.Size(756, 340);
            this.lstConfigFiles.TabIndex = 0;
            // 
            // btnLoadConfig
            // 
            this.btnLoadConfig.Location = new System.Drawing.Point(6, 371);
            this.btnLoadConfig.Name = "btnLoadConfig";
            this.btnLoadConfig.Size = new System.Drawing.Size(75, 23);
            this.btnLoadConfig.TabIndex = 1;
            this.btnLoadConfig.Text = "加载配置";
            this.btnLoadConfig.UseVisualStyleBackColor = true;
            this.btnLoadConfig.Click += new System.EventHandler(this.btnLoadConfig_Click);
            // 
            // btnCreateConfig
            // 
            this.btnCreateConfig.Location = new System.Drawing.Point(87, 371);
            this.btnCreateConfig.Name = "btnCreateConfig";
            this.btnCreateConfig.Size = new System.Drawing.Size(75, 23);
            this.btnCreateConfig.TabIndex = 2;
            this.btnCreateConfig.Text = "创建配置";
            this.btnCreateConfig.UseVisualStyleBackColor = true;
            this.btnCreateConfig.Click += new System.EventHandler(this.btnCreateConfig_Click);
            // 
            // btnDeleteConfig
            // 
            this.btnDeleteConfig.Location = new System.Drawing.Point(168, 371);
            this.btnDeleteConfig.Name = "btnDeleteConfig";
            this.btnDeleteConfig.Size = new System.Drawing.Size(75, 23);
            this.btnDeleteConfig.TabIndex = 3;
            this.btnDeleteConfig.Text = "删除配置";
            this.btnDeleteConfig.UseVisualStyleBackColor = true;
            this.btnDeleteConfig.Click += new System.EventHandler(this.btnDeleteConfig_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 9);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(53, 12);
            this.label4.TabIndex = 4;
            this.label4.Text = "配置文件";
            // 
            // btnRefreshConfig
            // 
            this.btnRefreshConfig.Location = new System.Drawing.Point(249, 371);
            this.btnRefreshConfig.Name = "btnRefreshConfig";
            this.btnRefreshConfig.Size = new System.Drawing.Size(75, 23);
            this.btnRefreshConfig.TabIndex = 5;
            this.btnRefreshConfig.Text = "刷新";
            this.btnRefreshConfig.UseVisualStyleBackColor = true;
            this.btnRefreshConfig.Click += new System.EventHandler(this.btnRefreshConfig_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 460);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(800, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(118, 17);
            this.toolStripStatusLabel1.Text = "就绪 - 等待操作";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 482);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.tabControl1);
            this.Name = "MainForm";
            this.Text = "Windows硬件信息模拟Hook工具";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.ListBox lstProcesses;
        private System.Windows.Forms.Button btnInject;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TextBox txtHardwareInfo;
        private System.Windows.Forms.Button btnReadHardware;
        private System.Windows.Forms.Button btnExportConfig;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Button btnReadLog;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.ListBox lstConfigFiles;
        private System.Windows.Forms.Button btnLoadConfig;
        private System.Windows.Forms.Button btnCreateConfig;
        private System.Windows.Forms.Button btnDeleteConfig;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btnRefreshConfig;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;

        /// <summary>
        /// 注入按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void btnInject_Click(object sender, EventArgs e)
        {
            InjectDll();
        }

        /// <summary>
        /// 刷新按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadProcesses();
        }

        /// <summary>
        /// 读取硬件按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void btnReadHardware_Click(object sender, EventArgs e)
        {
            ReadHardwareInfo();
        }

        /// <summary>
        /// 导出配置按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void btnExportConfig_Click(object sender, EventArgs e)
        {
            ExportConfig();
        }

        /// <summary>
        /// 读取日志按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void btnReadLog_Click(object sender, EventArgs e)
        {
            ReadLogs();
        }

        /// <summary>
        /// 加载配置文件列表
        /// </summary>
        private void LoadConfigFiles()
        {
            try
            {
                var configFiles = ConfigurationLoader.ListConfigurationFiles(_configDirectory);
                lstConfigFiles.Items.Clear();

                foreach (var configFile in configFiles)
                {
                    lstConfigFiles.Items.Add(configFile);
                }

                _logger.Info($"Loaded {configFiles.Length} configuration files");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to load configuration files", ex);
                MessageBox.Show("加载配置文件列表失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 加载配置按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void btnLoadConfig_Click(object sender, EventArgs e)
        {
            if (lstConfigFiles.SelectedItem == null)
            {
                MessageBox.Show("请选择一个配置文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                string selectedConfigFile = (string)lstConfigFiles.SelectedItem;
                var result = ConfigurationLoader.LoadConfiguration(selectedConfigFile);

                if (result.Success)
                {
                    // 复制选中的配置文件到默认配置路径
                    File.Copy(selectedConfigFile, _configPath, true);
                    _logger.Info($"Configuration loaded from {selectedConfigFile}");
                    MessageBox.Show("配置加载成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    _logger.Error("Failed to load configuration: " + result.ErrorMessage);
                    MessageBox.Show("配置加载失败: " + result.ErrorMessage, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to load configuration", ex);
                MessageBox.Show("配置加载失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 创建配置按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void btnCreateConfig_Click(object sender, EventArgs e)
        {
            try
            {
                // 读取当前硬件信息
                var snapshot = HardwareInfoReader.ReadHardwareInfo();
                if (!snapshot.IsSuccess)
                {
                    MessageBox.Show("读取硬件信息失败，无法创建配置", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 创建新的配置文件
                string configFileName = $"config_{DateTime.Now:yyyyMMddHHmmss}.json";
                string configFilePath = Path.Combine(_configDirectory, configFileName);

                var config = new HardwareConfig
                {
                    Cpu = new CpuConfig
                    {
                        Model = snapshot.CpuModel,
                        CoreCount = snapshot.CpuCoreCount,
                        CpuId = snapshot.CpuId
                    },
                    Disk = new DiskConfig
                    {
                        Serial = snapshot.DiskSerial
                    },
                    Mac = new MacConfig
                    {
                        Address = snapshot.MacAddress
                    },
                    Motherboard = new MotherboardConfig
                    {
                        Serial = snapshot.MotherboardSerial
                    }
                };

                if (ConfigurationLoader.SaveConfiguration(config, configFilePath))
                {
                    _logger.Info($"Configuration created at {configFilePath}");
                    MessageBox.Show("配置创建成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    // 刷新配置文件列表
                    LoadConfigFiles();
                }
                else
                {
                    _logger.Error("Failed to create configuration");
                    MessageBox.Show("配置创建失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to create configuration", ex);
                MessageBox.Show("配置创建失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 删除配置按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void btnDeleteConfig_Click(object sender, EventArgs e)
        {
            if (lstConfigFiles.SelectedItem == null)
            {
                MessageBox.Show("请选择一个配置文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                string selectedConfigFile = (string)lstConfigFiles.SelectedItem;
                if (MessageBox.Show($"确定要删除配置文件 {Path.GetFileName(selectedConfigFile)} 吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    File.Delete(selectedConfigFile);
                    _logger.Info($"Configuration deleted: {selectedConfigFile}");
                    MessageBox.Show("配置删除成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    // 刷新配置文件列表
                    LoadConfigFiles();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to delete configuration", ex);
                MessageBox.Show("配置删除失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 刷新配置列表按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void btnRefreshConfig_Click(object sender, EventArgs e)
        {
            LoadConfigFiles();
        }

        /// <summary>
        /// 窗体关闭事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 清理定时器
            _processRefreshTimer?.Dispose();

            _logger.Info("Application closed");
        }

        /// <summary>
        /// 进程项类
        /// </summary>
        private class ProcessItem
        {
            /// <summary>
            /// 进程ID
            /// </summary>
            public int ProcessId { get; set; }

            /// <summary>
            /// 进程名称
            /// </summary>
            public string ProcessName { get; set; }

            /// <summary>
            /// 窗口标题
            /// </summary>
            public string WindowTitle { get; set; }

            /// <summary>
            /// 是否已注入
            /// </summary>
            public bool IsInjected { get; set; }

            /// <summary>
            /// 转换为字符串
            /// </summary>
            /// <returns>进程信息字符串</returns>
            public override string ToString()
            {
                string windowTitle = string.IsNullOrEmpty(WindowTitle) ? "" : $" - {WindowTitle}";
                string injectedStatus = IsInjected ? " [已注入]" : "";
                return $"{ProcessName} (PID: {ProcessId}){windowTitle}{injectedStatus}";
            }
        }
    }
}
