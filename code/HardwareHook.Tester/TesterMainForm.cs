using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using HardwareHook.Core.HardwareInfo;
using HardwareHook.Core.Logging;

namespace HardwareHook.Tester
{
    internal sealed class TesterMainForm : Form
    {
        private readonly DataGridView _grid;
        private readonly Button _btnRefresh;
        private readonly Button _btnSave;
        private readonly StatusStrip _statusStrip;
        private readonly ToolStripStatusLabel _statusLabel;
        private readonly ILogger _logger;

        public TesterMainForm()
        {
            Text = "硬件信息测试工具";
            Size = new Size(620, 520);
            MinimumSize = new Size(500, 400);
            StartPosition = FormStartPosition.CenterScreen;

            _logger = new FileLogger(Environment.CurrentDirectory, "tester");

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                RowHeadersVisible = false
            };
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "属性", Width = 160 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "值", Width = 380 });

            var panel = new Panel { Dock = DockStyle.Bottom, Height = 50 };
            _btnRefresh = new Button
            {
                Text = "开始测试（刷新）",
                Size = new Size(130, 28),
                Location = new Point(12, 12)
            };
            _btnRefresh.Click += (s, e) => RefreshHardwareInfo();

            _btnSave = new Button
            {
                Text = "保存结果",
                Size = new Size(100, 28),
                Location = new Point(152, 12)
            };
            _btnSave.Click += (s, e) => SaveResult();

            _statusStrip = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel { Text = "就绪" };
            _statusStrip.Items.Add(_statusLabel);

            panel.Controls.Add(_btnSave);
            panel.Controls.Add(_btnRefresh);
            Controls.Add(_grid);
            Controls.Add(panel);
            Controls.Add(_statusStrip);

            Load += (s, e) => RefreshHardwareInfo();
        }

        private void RefreshHardwareInfo()
        {
            _statusLabel.Text = "正在读取硬件信息...";
            Application.DoEvents();

            try
            {
                var snapshot = HardwareInfoReader.GetCurrent(_logger);
                _grid.Rows.Clear();
                _grid.Rows.Add("处理器型号", snapshot.CpuModel);
                _grid.Rows.Add("核心数", snapshot.CpuCoreCount.ToString());
                _grid.Rows.Add("CpuId", snapshot.CpuId);
                _grid.Rows.Add("硬盘序列号", snapshot.DiskSerial);
                _grid.Rows.Add("MAC 地址", snapshot.MacAddress);
                _grid.Rows.Add("主板序列号", snapshot.MotherboardSerial);
                _statusLabel.Text = "已刷新。当前显示为本进程可见的硬件信息（若已注入 Hook 则为模拟值）。";
            }
            catch (Exception ex)
            {
                _logger.Error("读取硬件信息失败。", ex, "TesterUI");
                _statusLabel.Text = "读取失败: " + ex.Message;
                MessageBox.Show(this, "读取失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveResult()
        {
            if (_grid.Rows.Count == 0)
            {
                MessageBox.Show(this, "请先点击「开始测试」获取数据。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*";
                dlg.FileName = "hardware_info_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                dlg.Title = "保存结果";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("硬件信息测试结果");
                sb.AppendLine("时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine();
                for (int i = 0; i < _grid.Rows.Count; i++)
                {
                    var row = _grid.Rows[i];
                    if (row.Cells[0].Value == null) continue;
                    sb.AppendLine(row.Cells[0].Value + ": " + (row.Cells[1].Value ?? ""));
                }

                try
                {
                    File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                    _statusLabel.Text = "已保存: " + Path.GetFileName(dlg.FileName);
                    MessageBox.Show(this, "保存成功。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "保存失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
