using System;
using System.Drawing;
using System.Windows.Forms;

namespace MU3Input
{
    public partial class IOTest : Form
    {
        private HidIO _io;

        private CheckBox[] _left;
        private CheckBox[] _right;
        bool testDown = false;
        private object _data;
        private int ncoins;

        public IOTest(HidIO io)
        {
            InitializeComponent();

            _left = new[] {
                lA,
                lB,
                lC,
                lS,
                lM,
            };

            _right = new[] {
                rA,
                rB,
                rC,
                rS,
                rM,
            };

            _io = io;

            SetupLeverCalibration();
        }

        private Label _lblLever;
        private Button _btnCalStart;
        private Button _btnCalSave;
        private CheckBox _chkInvert;

        private void SetupLeverCalibration()
        {
            var top = ClientSize.Height;
            ClientSize = new Size(ClientSize.Width, top + 76);

            _lblLever = new Label
            {
                Location = new Point(12, top + 4),
                Size = new Size(ClientSize.Width - 24, 36),
                Font = new Font(FontFamily.GenericMonospace, 8.5f),
            };

            _btnCalStart = new Button
            {
                Location = new Point(12, top + 44),
                Size = new Size(110, 26),
                Text = "开始摇杆校准",
            };
            _btnCalStart.Click += (s, e) =>
            {
                _io.Calibration.ResetSeen();
                _io.Calibration.Calibrating = true;
                MessageBox.Show("把摇杆缓慢拨到最左、再拨到最右（各到底一次），然后点「保存校准」。", "摇杆校准");
            };

            _btnCalSave = new Button
            {
                Location = new Point(130, top + 44),
                Size = new Size(110, 26),
                Text = "保存校准",
            };
            _btnCalSave.Click += (s, e) =>
            {
                var cal = _io.Calibration;
                if (!cal.SaveSeen())
                {
                    MessageBox.Show("没有检测到足够的摇杆行程，请先点「开始摇杆校准」并把摇杆拨到两端。", "摇杆校准");
                    return;
                }

                cal.Calibrating = false;
                MessageBox.Show(string.Format("已保存：min={0} max={1}\n记得在游戏测试菜单里重新做一次摇杆校准。", cal.Min, cal.Max), "摇杆校准");
            };

            _chkInvert = new CheckBox
            {
                Location = new Point(252, top + 48),
                Size = new Size(120, 20),
                Text = "反转方向",
                Checked = _io.Calibration.Invert,
            };
            _chkInvert.CheckedChanged += (s, e) =>
            {
                _io.Calibration.Invert = _chkInvert.Checked;
                _io.Calibration.Save();
            };

            Controls.Add(_lblLever);
            Controls.Add(_btnCalStart);
            Controls.Add(_btnCalSave);
            Controls.Add(_chkInvert);
        }

        private void UpdateLeverInfo()
        {
            var cal = _io.Calibration;
            var report = _io.LastReport;
            var mode = cal.Calibrating ? "校准中" : cal.HasSaved ? "已校准" : "自动";
            var seen = cal.SeenMax >= cal.SeenMin ? cal.SeenMin + "~" + cal.SeenMax : "-";

            _lblLever.Text = string.Format(
                "摇杆 原始={0} 已扫过={1} 保存={2}~{3} 输出={4} [{5}]\nHID: {6}",
                cal.Raw, seen, cal.Min, cal.Max, _io.Lever, mode,
                BitConverter.ToString(report, 0, 16));
        }
        
        public static byte[] StringToByteArray(string hex)
        {
            var numberChars = hex.Length;
            var bytes = new byte[numberChars / 2];
            for (var i = 0; i < numberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        internal void UpdateData()
        {
            if (!Enabled && Handle == IntPtr.Zero) return;

            try
            {
                BeginInvoke(new Action(() =>
                {
                    lblStatus.Text = _io.IsConnected ? "Nageki 已连接" : "Nageki 未连接";

                    if (!_io.IsConnected) return;
                    

                    for (var i = 0; i < 5; i++)
                    {
                        if (i == 3)
                        {
                            _left[i].Checked = (Convert.ToBoolean(_io.Data.Buttons[i]));
                            _right[i].Checked = (Convert.ToBoolean(_io.Data.Buttons[i + 5]));
                        }
                        else
                        {
                            _left[i].Checked = Convert.ToBoolean(_io.Data.Buttons[i]);
                            _right[i].Checked = Convert.ToBoolean(_io.Data.Buttons[i + 5]);
                        }

                    }


                    trackBar1.Value = Math.Max(trackBar1.Minimum, Math.Min(trackBar1.Maximum, (int) _io.Lever));
                    UpdateLeverInfo();

                    if (_io.Scan)
                    {
                        textAimiId.Text = BitConverter.ToString(_io.AimiId).Replace("-", "");
                    }
                }));
            }
            catch
            {
                // ignored
            }
        }
        
        public void SetColor(uint data)
        {
            try
            {
                BeginInvoke(new Action(() =>
                {
                    _left[0].BackColor = Color.FromArgb(
                        (int)((data >> 23) & 1) * 255,
                        (int)((data >> 19) & 1) * 255,
                        (int)((data >> 22) & 1) * 255
                    );
                    _left[1].BackColor = Color.FromArgb(
                        (int)((data >> 20) & 1) * 255,
                        (int)((data >> 21) & 1) * 255,
                        (int)((data >> 18) & 1) * 255
                    );
                    _left[2].BackColor = Color.FromArgb(
                        (int)((data >> 17) & 1) * 255,
                        (int)((data >> 16) & 1) * 255,
                        (int)((data >> 15) & 1) * 255
                    );
                    _right[0].BackColor = Color.FromArgb(
                        (int)((data >> 14) & 1) * 255,
                        (int)((data >> 13) & 1) * 255,
                        (int)((data >> 12) & 1) * 255
                    );
                    _right[1].BackColor = Color.FromArgb(
                        (int)((data >> 11) & 1) * 255,
                        (int)((data >> 10) & 1) * 255,
                        (int)((data >> 9) & 1) * 255
                    );
                    _right[2].BackColor = Color.FromArgb(
                        (int)((data >> 8) & 1) * 255,
                        (int)((data >> 7) & 1) * 255,
                        (int)((data >> 6) & 1) * 255
                    );
                }));
            }
            catch
            {
                // ignored
            }
        }
        


    private void btnSetOption_Click(object sender, EventArgs e)
        {
            byte[] aimiId;
            MessageBox.Show("已写入卡号，请长按menu刷卡.");
            try
            {
                aimiId = StringToByteArray(textAimiId.Text);
            }
            catch
            {
                MessageBox.Show("无效卡号，卡号需要20个数字组成.", "错误");
                return;
            }
            
            if (aimiId.Length != 10)
            {
                MessageBox.Show("无效卡号，卡号需要20个数字组成.");
                return;
            }

            _io.SetAimiId(aimiId);
        }

        

        private void lblStatus_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

        private void rB_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void rS_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void IOTest_Load(object sender, EventArgs e)
        {

        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            
            
        }

        private void Test_Click(object sender, EventArgs e)
        {

        }

        private void Service_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void lA_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void rA_CheckedChanged(object sender, EventArgs e)
        {

        }


        private void button1_Click_1(object sender, EventArgs e)
        {
            _io.TestButton = 1;
            return;

        }

        private void rC_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            
        }

        private void lB_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void Test2_Click(object sender, EventArgs e)
        {
            _io.TestButton = 2;
            return;
        }

        private void Test3_Click(object sender, EventArgs e)
        {
            _io.TestButton = 3;
            return;
        }

        private void lS_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void rM_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void textAimiId_TextChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click_2(object sender, EventArgs e)
        {
            
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Aime_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }

        private void button1_Click_3(object sender, EventArgs e)
        {
            label1.BackColor = Color.FromArgb(0, 0, 0);
        }
    }
}
