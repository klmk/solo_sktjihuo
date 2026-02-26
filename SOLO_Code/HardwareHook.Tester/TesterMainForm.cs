using System;
using System.Windows.Forms;
using HardwareHook.Core.HardwareInfo;

namespace HardwareHook.Tester
{
    /// <summary>
    /// 测试程序主窗体
    /// </summary>
    public partial class TesterMainForm : Form
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public TesterMainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 测试CPU信息
        /// </summary>
        private void TestCpuInfo()
        {
            var snapshot = HardwareInfoReader.ReadHardwareInfo();
            txtTestResult.Text = "=== CPU 信息测试 ===\n";
            txtTestResult.Text += $"CPU型号: {snapshot.CpuModel}\n";
            txtTestResult.Text += $"核心数: {snapshot.CpuCoreCount}\n";
            txtTestResult.Text += $"CPU ID: {snapshot.CpuId}\n";
        }

        /// <summary>
        /// 测试硬盘信息
        /// </summary>
        private void TestDiskInfo()
        {
            var snapshot = HardwareInfoReader.ReadHardwareInfo();
            txtTestResult.Text = "=== 硬盘 信息测试 ===\n";
            txtTestResult.Text += $"硬盘序列号: {snapshot.DiskSerial}\n";
        }

        /// <summary>
        /// 测试MAC地址信息
        /// </summary>
        private void TestMacInfo()
        {
            var snapshot = HardwareInfoReader.ReadHardwareInfo();
            txtTestResult.Text = "=== MAC 地址测试 ===\n";
            txtTestResult.Text += $"MAC地址: {snapshot.MacAddress}\n";
        }

        /// <summary>
        /// 测试主板信息
        /// </summary>
        private void TestMotherboardInfo()
        {
            var snapshot = HardwareInfoReader.ReadHardwareInfo();
            txtTestResult.Text = "=== 主板 信息测试 ===\n";
            txtTestResult.Text += $"主板序列号: {snapshot.MotherboardSerial}\n";
        }

        /// <summary>
        /// 测试所有信息
        /// </summary>
        private void TestAllInfo()
        {
            var snapshot = HardwareInfoReader.ReadHardwareInfo();
            txtTestResult.Text = "=== 所有硬件信息测试 ===\n";
            txtTestResult.Text += snapshot.ToString();
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponent()
        {
            this.btnTestCpu = new System.Windows.Forms.Button();
            this.btnTestDisk = new System.Windows.Forms.Button();
            this.btnTestMac = new System.Windows.Forms.Button();
            this.btnTestMotherboard = new System.Windows.Forms.Button();
            this.btnTestAll = new System.Windows.Forms.Button();
            this.txtTestResult = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnTestCpu
            // 
            this.btnTestCpu.Location = new System.Drawing.Point(12, 30);
            this.btnTestCpu.Name = "btnTestCpu";
            this.btnTestCpu.Size = new System.Drawing.Size(75, 23);
            this.btnTestCpu.TabIndex = 0;
            this.btnTestCpu.Text = "测试CPU";
            this.btnTestCpu.UseVisualStyleBackColor = true;
            this.btnTestCpu.Click += new System.EventHandler(this.btnTestCpu_Click);
            // 
            // btnTestDisk
            // 
            this.btnTestDisk.Location = new System.Drawing.Point(93, 30);
            this.btnTestDisk.Name = "btnTestDisk";
            this.btnTestDisk.Size = new System.Drawing.Size(75, 23);
            this.btnTestDisk.TabIndex = 1;
            this.btnTestDisk.Text = "测试硬盘";
            this.btnTestDisk.UseVisualStyleBackColor = true;
            this.btnTestDisk.Click += new System.EventHandler(this.btnTestDisk_Click);
            // 
            // btnTestMac
            // 
            this.btnTestMac.Location = new System.Drawing.Point(174, 30);
            this.btnTestMac.Name = "btnTestMac";
            this.btnTestMac.Size = new System.Drawing.Size(75, 23);
            this.btnTestMac.TabIndex = 2;
            this.btnTestMac.Text = "测试MAC";
            this.btnTestMac.UseVisualStyleBackColor = true;
            this.btnTestMac.Click += new System.EventHandler(this.btnTestMac_Click);
            // 
            // btnTestMotherboard
            // 
            this.btnTestMotherboard.Location = new System.Drawing.Point(255, 30);
            this.btnTestMotherboard.Name = "btnTestMotherboard";
            this.btnTestMotherboard.Size = new System.Drawing.Size(75, 23);
            this.btnTestMotherboard.TabIndex = 3;
            this.btnTestMotherboard.Text = "测试主板";
            this.btnTestMotherboard.UseVisualStyleBackColor = true;
            this.btnTestMotherboard.Click += new System.EventHandler(this.btnTestMotherboard_Click);
            // 
            // btnTestAll
            // 
            this.btnTestAll.Location = new System.Drawing.Point(336, 30);
            this.btnTestAll.Name = "btnTestAll";
            this.btnTestAll.Size = new System.Drawing.Size(75, 23);
            this.btnTestAll.TabIndex = 4;
            this.btnTestAll.Text = "测试全部";
            this.btnTestAll.UseVisualStyleBackColor = true;
            this.btnTestAll.Click += new System.EventHandler(this.btnTestAll_Click);
            // 
            // txtTestResult
            // 
            this.txtTestResult.Location = new System.Drawing.Point(12, 59);
            this.txtTestResult.Multiline = true;
            this.txtTestResult.Name = "txtTestResult";
            this.txtTestResult.ReadOnly = true;
            this.txtTestResult.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtTestResult.Size = new System.Drawing.Size(400, 200);
            this.txtTestResult.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 12);
            this.label1.TabIndex = 6;
            this.label1.Text = "硬件信息测试";
            // 
            // TesterMainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(424, 271);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtTestResult);
            this.Controls.Add(this.btnTestAll);
            this.Controls.Add(this.btnTestMotherboard);
            this.Controls.Add(this.btnTestMac);
            this.Controls.Add(this.btnTestDisk);
            this.Controls.Add(this.btnTestCpu);
            this.Name = "TesterMainForm";
            this.Text = "硬件信息测试工具";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Button btnTestCpu;
        private System.Windows.Forms.Button btnTestDisk;
        private System.Windows.Forms.Button btnTestMac;
        private System.Windows.Forms.Button btnTestMotherboard;
        private System.Windows.Forms.Button btnTestAll;
        private System.Windows.Forms.TextBox txtTestResult;
        private System.Windows.Forms.Label label1;

        /// <summary>
        /// 测试CPU按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void btnTestCpu_Click(object sender, EventArgs e)
        {
            TestCpuInfo();
        }

        /// <summary>
        /// 测试硬盘按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void btnTestDisk_Click(object sender, EventArgs e)
        {
            TestDiskInfo();
        }

        /// <summary>
        /// 测试MAC按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void btnTestMac_Click(object sender, EventArgs e)
        {
            TestMacInfo();
        }

        /// <summary>
        /// 测试主板按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void btnTestMotherboard_Click(object sender, EventArgs e)
        {
            TestMotherboardInfo();
        }

        /// <summary>
        /// 测试全部按钮点击事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void btnTestAll_Click(object sender, EventArgs e)
        {
            TestAllInfo();
        }
    }
}
