using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("VlynnByeDPI GUI")]
[assembly: System.Reflection.AssemblyProduct("VlynnByeDPI")]
[assembly: System.Reflection.AssemblyCopyright("Copyright © Vlynn")]
[assembly: System.Runtime.InteropServices.ComVisible(false)]

namespace VlynnByeDPI
{
    public class MainForm : Form
    {
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private Process _engineProcess;
        private readonly string _appDir;
        private readonly string _archDir;
        private readonly string _enginePath;
        private Icon _coloredIcon;
        private Icon _grayscaleIcon;
        private Label _statusLabel;
        private Font _titleFont;
        private Font _statusFont;
        private Font _buttonFont;
        private Button _btnMode1;
        private Button _btnMode2;
        private Button _btnMode3;
        private Button _btnMode4;
        private Button _btnStop;
        private Button _btnSettings;
        private bool _isSystemDnsChanged = false;

        public MainForm()
        {
            _appDir = AppDomain.CurrentDomain.BaseDirectory;
            string arch = Environment.Is64BitOperatingSystem ? "x86_64" : "x86";
            _archDir = Path.Combine(_appDir, arch);
            _enginePath = Path.Combine(_archDir, "vlynnengine.exe");

            InitializeComponents();
            LoadIcons();
            
            string configPath = Path.Combine(_appDir, "config.ini");
            if (File.Exists(configPath))
            {
                string configContent = File.ReadAllText(configPath);
                if (configContent.Contains("AutoConnect=True"))
                {
                    if (configContent.Contains("StartupMode=1")) StartMode1(null, null);
                    else if (configContent.Contains("StartupMode=2")) StartMode2(null, null);
                    else if (configContent.Contains("StartupMode=4")) StartMode4(null, null);
                    else StartMode3(null, null); // Default
                }
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            if (Program.StartMinimized && !this.IsHandleCreated)
            {
                value = false;
                CreateHandle();
            }
            base.SetVisibleCore(value);
        }

        private void InitializeComponents()
        {
            this.Text = "VlynnByeDPI - Control Panel";
            this.Size = new Size(350, 320);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;

            _titleFont = new Font("Segoe UI", 16, FontStyle.Bold);
            _statusFont = new Font("Segoe UI", 10, FontStyle.Bold);
            _buttonFont = new Font("Segoe UI", 9, FontStyle.Regular);

            Label titleLabel = new Label();
            titleLabel.Text = "VlynnByeDPI";
            titleLabel.Font = _titleFont;
            titleLabel.AutoSize = true;
            titleLabel.Location = new Point(100, 15);
            this.Controls.Add(titleLabel);

            _statusLabel = new Label();
            _statusLabel.Text = "Status: OFF";
            _statusLabel.Font = _statusFont;
            _statusLabel.ForeColor = Color.Gray;
            _statusLabel.AutoSize = false;
            _statusLabel.Width = this.Width;
            _statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            _statusLabel.Location = new Point(0, 50);
            this.Controls.Add(_statusLabel);

            _btnMode1 = CreateButton("1. AdBlock DNS & Bypass", 80, StartMode1);
            _btnMode2 = CreateButton("2. Unfiltered DNS (Max Compatibility)", 120, StartMode2);
            _btnMode3 = CreateButton("3. Gaming & Safe DNS (Recommended)", 160, StartMode3);
            _btnMode4 = CreateButton("4. Custom DNS (Power User)", 200, StartMode4);
            
            _btnStop = CreateButton("STOP", 250, StopEngineClick);
            _btnStop.BackColor = Color.FromArgb(200, 50, 50);

            _btnSettings = new Button();
            _btnSettings.Text = "⚙";
            _btnSettings.Font = new Font("Segoe UI", 12, FontStyle.Regular);
            _btnSettings.Size = new Size(35, 35);
            _btnSettings.Location = new Point(290, 15);
            _btnSettings.FlatStyle = FlatStyle.Flat;
            _btnSettings.BackColor = Color.FromArgb(60, 60, 60);
            _btnSettings.Cursor = Cursors.Hand;
            _btnSettings.Click += (s, e) => {
                using (SettingsForm sf = new SettingsForm(_appDir))
                {
                    sf.ShowDialog();
                }
            };
            this.Controls.Add(_btnSettings);

            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.Add("Show Control Panel", null, (s, e) => ShowForm());
            _trayMenu.Items.Add("-");
            _trayMenu.Items.Add("Mode 1: AdBlock DNS & Bypass", null, (s, e) => StartMode1(s, e));
            _trayMenu.Items.Add("Mode 2: Unfiltered DNS", null, (s, e) => StartMode2(s, e));
            _trayMenu.Items.Add("Mode 3: Gaming & Safe DNS", null, (s, e) => StartMode3(s, e));
            _trayMenu.Items.Add("Mode 4: Custom DNS", null, (s, e) => StartMode4(s, e));
            _trayMenu.Items.Add("-");
            _trayMenu.Items.Add("Stop Engine", null, (s, e) => StopEngineClick(s, e));
            _trayMenu.Items.Add("-");
            _trayMenu.Items.Add("Exit completely", null, OnExit);

            _trayIcon = new NotifyIcon();
            _trayIcon.Text = "VlynnByeDPI (OFF)";
            _trayIcon.ContextMenuStrip = _trayMenu;
            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += (s, e) => ShowForm();

            this.FormClosing += MainForm_FormClosing;
        }

        private Button CreateButton(string text, int y, EventHandler onClick)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = new Point(50, y);
            btn.Size = new Size(230, 35);
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = Color.FromArgb(60, 60, 60);
            btn.Font = _buttonFont;
            btn.Cursor = Cursors.Hand;
            btn.Click += onClick;
            this.Controls.Add(btn);
            return btn;
        }

        private void LoadIcons()
        {
            string iconPath = Path.Combine(_archDir, "icon.ico");
            if (File.Exists(iconPath))
            {
                try
                {
                    _coloredIcon = new Icon(iconPath);
                    this.Icon = _coloredIcon;
                    
                    using (Bitmap bmp = new Bitmap(iconPath))
                    {
                        _grayscaleIcon = MakeGrayscaleIcon(bmp);
                    }
                    UpdateStatus(false);
                }
                catch (Exception ex) { File.AppendAllText("error.log", ex.ToString() + Environment.NewLine); }
            }
        }

        private Icon MakeGrayscaleIcon(Bitmap original)
        {
            Bitmap gray = new Bitmap(original.Width, original.Height);
            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    Color c = original.GetPixel(x, y);
                    if (c.A > 0)
                    {
                        int luma = (int)(c.R * 0.3 + c.G * 0.59 + c.B * 0.11);
                        gray.SetPixel(x, y, Color.FromArgb(c.A, luma, luma, luma));
                    }
                    else
                    {
                        gray.SetPixel(x, y, Color.Transparent);
                    }
                }
            }
            Icon grayIcon = Icon.FromHandle(gray.GetHicon());
            gray.Dispose();
            return grayIcon;
        }

        private void SetButtonsEnabled(bool enabled)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<bool>(SetButtonsEnabled), enabled); return; }
            _btnMode1.Enabled = enabled;
            _btnMode2.Enabled = enabled;
            _btnMode3.Enabled = enabled;
            _btnMode4.Enabled = enabled;
            _btnStop.Enabled = enabled;
            _btnSettings.Enabled = enabled;
        }

        private void TransitionToMode(Action backgroundAction, string labelName)
        {
            SetButtonsEnabled(false);
            if (this.InvokeRequired) { this.Invoke(new Action(() => { _statusLabel.Text = "⚡ Connecting to " + labelName + "..."; _statusLabel.ForeColor = Color.Orange; if (_trayIcon != null) _trayIcon.Text = "VlynnByeDPI (Connecting...)"; })); }
            else { _statusLabel.Text = "⚡ Connecting to " + labelName + "..."; _statusLabel.ForeColor = Color.Orange; if (_trayIcon != null) _trayIcon.Text = "VlynnByeDPI (Connecting...)"; }

            System.Threading.ThreadPool.QueueUserWorkItem((state) =>
            {
                System.Threading.Thread.Sleep(1500); // 1.5s smooth transition
                backgroundAction();
                SetButtonsEnabled(true);
            });
        }

        private void StopEngineClick(object sender, EventArgs e) { TransitionToMode(() => StopEngine(sender, e), "System Stop"); }

        private void StartMode1(object sender, EventArgs e)
        {
            TransitionToMode(() => {
                StopEngine(null, null);
                try {
                    string psCmd = "Add-DnsClientDoHServerAddress -ServerAddress '94.140.14.14' -DoHTemplate 'https://dns.adguard-dns.com/dns-query' -AllowFallbackToUdp $False -ErrorAction SilentlyContinue; " +
                                   "Add-DnsClientDoHServerAddress -ServerAddress '94.140.15.15' -DoHTemplate 'https://dns.adguard-dns.com/dns-query' -AllowFallbackToUdp $False -ErrorAction SilentlyContinue; " +
                                   "Add-DnsClientDoHServerAddress -ServerAddress '2a10:50c0::ad1:ff' -DoHTemplate 'https://dns.adguard-dns.com/dns-query' -AllowFallbackToUdp $False -ErrorAction SilentlyContinue; " +
                                   "Add-DnsClientDoHServerAddress -ServerAddress '2a10:50c0::ad2:ff' -DoHTemplate 'https://dns.adguard-dns.com/dns-query' -AllowFallbackToUdp $False -ErrorAction SilentlyContinue; " +
                                   "Get-NetAdapter | Where-Object Status -eq 'Up' | Set-DnsClientServerAddress -ServerAddresses ('94.140.14.14','94.140.15.15','2a10:50c0::ad1:ff','2a10:50c0::ad2:ff'); ipconfig /flushdns";
                    ProcessStartInfo psi = new ProcessStartInfo("powershell", "-WindowStyle Hidden -Command \"" + psCmd + "\"");
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    Process p = Process.Start(psi);
                    p.WaitForExit();
                    _isSystemDnsChanged = true;
                } catch (Exception ex) { File.AppendAllText("error.log", "DNS Change Error: " + ex.ToString() + Environment.NewLine); }
                UpdateStatus(true, "Mode 1 (AdBlock DoH)");
            }, "Mode 1");
        }
        private void StartMode2(object sender, EventArgs e) { TransitionToMode(() => StartEngine("-6 --dns-addr 77.88.8.8 --dns-port 1253 --dnsv6-addr 2a02:6b8::feed:0ff --dnsv6-port 1253", "Mode 2"), "Mode 2"); }
        private void StartMode3(object sender, EventArgs e) { TransitionToMode(() => StartEngine("-6 --dns-addr 77.88.8.1 --dns-port 1253 --dnsv6-addr 2a02:6b8::feed:bad --dnsv6-port 1253", "Mode 3"), "Mode 3"); }
        private void StartMode4(object sender, EventArgs e)
        {
            if (Program.StartMinimized && sender == null)
            {
                string lastFile = Path.Combine(_appDir, "last_dns.txt");
                string ip4 = File.Exists(lastFile) ? File.ReadAllText(lastFile).Trim() : "8.8.8.8";
                string ip6 = "2606:4700:4700::1111";
                if (ip4 == "8.8.8.8" || ip4 == "8.8.4.4") ip6 = "2001:4860:4860::8888";
                else if (ip4 == "9.9.9.9" || ip4 == "149.112.112.112") ip6 = "2620:fe::fe";
                else if (ip4 == "208.67.222.222" || ip4 == "208.67.220.220") ip6 = "2620:119:35::35";
                
                TransitionToMode(() => StartEngine(string.Format("-6 --dns-addr {0} --dns-port 5353 --dnsv6-addr {1} --dnsv6-port 5353", ip4, ip6), "Mode 4 (" + ip4 + ")"), "Mode 4");
                return;
            }

            using (Form prompt = new Form())
            {
                prompt.Width = 380;
                prompt.Height = 160;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = "Custom DNS Mode";
                prompt.StartPosition = FormStartPosition.CenterScreen;
                prompt.BackColor = Color.FromArgb(30, 30, 30);
                prompt.ForeColor = Color.White;
                prompt.MaximizeBox = false;

                Label textLabel = new Label() { Left = 20, Top = 20, Width = 300, Text = "Select DNS Server:" };
                ComboBox comboBox = new ComboBox() { Left = 20, Top = 50, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
                comboBox.Items.AddRange(new string[] { "8.8.8.8 (Google)", "1.1.1.1 (Cloudflare)", "9.9.9.9 (Quad9)", "208.67.222.222 (OpenDNS)" });
                
                string lastDnsFile = Path.Combine(_appDir, "last_dns.txt");
                if (File.Exists(lastDnsFile)) {
                    string last = File.ReadAllText(lastDnsFile).Trim();
                    int idx = comboBox.FindString(last);
                    comboBox.SelectedIndex = idx >= 0 ? idx : 0;
                } else {
                    comboBox.SelectedIndex = 0;
                }

                Button confirmation = new Button() { Text = "Connect", Left = 240, Width = 80, Top = 48, BackColor = Color.FromArgb(60, 60, 60), FlatStyle = FlatStyle.Flat };
                confirmation.DialogResult = DialogResult.OK;

                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(comboBox);
                prompt.Controls.Add(confirmation);
                prompt.AcceptButton = confirmation;

                if (prompt.ShowDialog() == DialogResult.OK && comboBox.SelectedItem != null)
                {
                    string selectedText = comboBox.SelectedItem.ToString();
                    string ip4 = selectedText.Split(' ')[0].Trim();
                    string ip6 = "2606:4700:4700::1111"; // Cloudflare fallback
                    if (ip4 == "8.8.8.8" || ip4 == "8.8.4.4") ip6 = "2001:4860:4860::8888";
                    else if (ip4 == "9.9.9.9" || ip4 == "149.112.112.112") ip6 = "2620:fe::fe";
                    else if (ip4 == "208.67.222.222" || ip4 == "208.67.220.220") ip6 = "2620:119:35::35";

                    File.WriteAllText(lastDnsFile, ip4);
                    TransitionToMode(() => StartEngine(string.Format("-6 --dns-addr {0} --dns-port 5353 --dnsv6-addr {1} --dnsv6-port 5353", ip4, ip6), "Mode 4 (" + ip4 + ")"), "Mode 4");
                }
            }
        }

        private void StartEngine(string args, string modeName)
        {
            StopEngine(null, null);
            if (!File.Exists(_enginePath)) return;

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = _enginePath;
                psi.Arguments = args;
                psi.WorkingDirectory = _archDir;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.CreateNoWindow = true; // Set to true to make the console completely invisible
                psi.UseShellExecute = false;

                _engineProcess = Process.Start(psi);
                UpdateStatus(true, modeName);
            }
            catch (Exception ex) { File.AppendAllText("error.log", ex.ToString() + Environment.NewLine); }
        }

        private void StopEngine(object sender, EventArgs e)
        {
            if (_isSystemDnsChanged)
            {
                try {
                    ProcessStartInfo psi = new ProcessStartInfo("powershell", "-WindowStyle Hidden -Command \"Get-NetAdapter | Where-Object Status -eq 'Up' | Set-DnsClientServerAddress -ResetServerAddresses; ipconfig /flushdns\"");
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    Process p = Process.Start(psi);
                    p.WaitForExit();
                    _isSystemDnsChanged = false;
                } catch (Exception ex) { File.AppendAllText("error.log", "DNS Reset Error: " + ex.ToString() + Environment.NewLine); }
            }

            try
            {
                Process[] procs = Process.GetProcessesByName("vlynnengine");
                foreach (var p in procs)
                {
                    p.Kill();
                    p.Dispose();
                }
            }
            catch (Exception ex) { File.AppendAllText("error.log", ex.ToString() + Environment.NewLine); }
            
            if (_engineProcess != null)
            {
                _engineProcess.Dispose();
                _engineProcess = null;
            }
            UpdateStatus(false);
        }

        private void UpdateStatus(bool isRunning, string modeName = "")
        {
            if (this.InvokeRequired) { this.Invoke(new Action<bool, string>(UpdateStatus), isRunning, modeName); return; }

            if (isRunning)
            {
                _statusLabel.Text = "Status: RUNNING - " + modeName;
                
                if (modeName.Contains("Mode 1")) _statusLabel.ForeColor = Color.LimeGreen;
                else if (modeName.Contains("Mode 2")) _statusLabel.ForeColor = Color.OrangeRed;
                else if (modeName.Contains("Mode 3")) _statusLabel.ForeColor = Color.Cyan;
                else if (modeName.Contains("Mode 4")) _statusLabel.ForeColor = Color.HotPink;
                else _statusLabel.ForeColor = Color.LimeGreen;

                if (_coloredIcon != null) _trayIcon.Icon = _coloredIcon;
                _trayIcon.Text = "VlynnByeDPI (ON) - " + modeName;

                foreach (ToolStripItem item in _trayMenu.Items)
                {
                    ToolStripMenuItem menuItem = item as ToolStripMenuItem;
                    if (menuItem != null && menuItem.Text.StartsWith("Mode"))
                    {
                        string baseMode = modeName.Contains(" (") ? modeName.Substring(0, modeName.IndexOf(" (")) : modeName;
                        menuItem.Checked = menuItem.Text.StartsWith(baseMode);
                    }
                }
            }
            else
            {
                _statusLabel.Text = "Status: OFF";
                _statusLabel.ForeColor = Color.Gray;
                if (_grayscaleIcon != null) _trayIcon.Icon = _grayscaleIcon;
                else if (_coloredIcon != null) _trayIcon.Icon = _coloredIcon;
                _trayIcon.Text = "VlynnByeDPI (OFF)";

                foreach (ToolStripItem item in _trayMenu.Items)
                {
                    ToolStripMenuItem menuItem = item as ToolStripMenuItem;
                    if (menuItem != null && menuItem.Text.StartsWith("Mode"))
                    {
                        menuItem.Checked = false;
                    }
                }
            }
        }

        private void ShowForm()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                _trayIcon.ShowBalloonTip(1000, "VlynnByeDPI", "The app is still running in the tray.", ToolTipIcon.Info);
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            StopEngine(null, null);
            _trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_trayIcon != null) _trayIcon.Dispose();
                if (_coloredIcon != null) _coloredIcon.Dispose();
                if (_grayscaleIcon != null) _grayscaleIcon.Dispose();
                if (_titleFont != null) _titleFont.Dispose();
                if (_statusFont != null) _statusFont.Dispose();
                if (_buttonFont != null) _buttonFont.Dispose();
                if (_btnSettings != null && _btnSettings.Font != null) _btnSettings.Font.Dispose();
                if (_engineProcess != null) _engineProcess.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class SettingsForm : Form
    {
        private readonly string _appDir;
        private readonly string _exePath;
        private CheckBox _chkStartup;

        public SettingsForm(string _appDir)
        {
            this._appDir = _appDir;
            this._exePath = Process.GetCurrentProcess().MainModule.FileName;

            this.Text = "Settings";
            this.Size = new Size(300, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;

            _chkStartup = new CheckBox();
            _chkStartup.Text = "Start with Windows (Auto-start)";
            _chkStartup.Location = new Point(30, 30);
            _chkStartup.AutoSize = true;
            _chkStartup.Checked = CheckStartupTask();
            _chkStartup.CheckedChanged += ChkStartup_CheckedChanged;
            this.Controls.Add(_chkStartup);

            CheckBox chkAutoConnect = new CheckBox();
            chkAutoConnect.Text = "Auto-connect on startup";
            chkAutoConnect.Location = new Point(30, 60);
            chkAutoConnect.AutoSize = true;
            
            ComboBox cmbStartupMode = new ComboBox();
            cmbStartupMode.Items.AddRange(new string[] { "Mode 1", "Mode 2", "Mode 3", "Mode 4" });
            cmbStartupMode.Location = new Point(190, 58);
            cmbStartupMode.Width = 80;
            cmbStartupMode.DropDownStyle = ComboBoxStyle.DropDownList;

            string configPath = Path.Combine(_appDir, "config.ini");
            string configContent = File.Exists(configPath) ? File.ReadAllText(configPath) : "";
            chkAutoConnect.Checked = configContent.Contains("AutoConnect=True");
            
            if (configContent.Contains("StartupMode=1")) cmbStartupMode.SelectedIndex = 0;
            else if (configContent.Contains("StartupMode=2")) cmbStartupMode.SelectedIndex = 1;
            else if (configContent.Contains("StartupMode=4")) cmbStartupMode.SelectedIndex = 3;
            else cmbStartupMode.SelectedIndex = 2; // Default to Mode 3

            Action saveConfig = () => {
                File.WriteAllText(configPath, "AutoConnect=" + chkAutoConnect.Checked.ToString() + Environment.NewLine + "StartupMode=" + (cmbStartupMode.SelectedIndex + 1).ToString());
            };

            chkAutoConnect.CheckedChanged += (s, e) => saveConfig();
            cmbStartupMode.SelectedIndexChanged += (s, e) => saveConfig();

            this.Controls.Add(chkAutoConnect);
            this.Controls.Add(cmbStartupMode);

            Button btnClose = new Button();
            btnClose.Text = "Close";
            btnClose.Location = new Point(100, 120);
            btnClose.Size = new Size(80, 30);
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.BackColor = Color.FromArgb(60, 60, 60);
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }

        private bool CheckStartupTask()
        {
            try
            {
                using (Process p = new Process())
                {
                    p.StartInfo.FileName = "schtasks.exe";
                    p.StartInfo.Arguments = "/query /tn \"VlynnByeDPI_AutoStart\"";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.Start();
                    string outStr = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    return outStr.Contains("VlynnByeDPI_AutoStart");
                }
            }
            catch { return false; }
        }

        private void ChkStartup_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (_chkStartup.Checked)
                {
                    using (Process p = new Process())
                    {
                        p.StartInfo.FileName = "schtasks.exe";
                        p.StartInfo.Arguments = "/create /tn \"VlynnByeDPI_AutoStart\" /tr \"\\\"" + _exePath + "\\\" --minimized\" /sc onlogon /rl highest /f";
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.CreateNoWindow = true;
                        p.Start();
                        p.WaitForExit();
                    }
                }
                else
                {
                    using (Process p = new Process())
                    {
                        p.StartInfo.FileName = "schtasks.exe";
                        p.StartInfo.Arguments = "/delete /tn \"VlynnByeDPI_AutoStart\" /f";
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.CreateNoWindow = true;
                        p.Start();
                        p.WaitForExit();
                    }
                }
            }
            catch 
            {
                _chkStartup.Checked = !_chkStartup.Checked; // Revert visually
            }
        }
    }

    static class Program
    {
        public static bool StartMinimized = false;

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--minimized")
            {
                StartMinimized = true;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                File.WriteAllText("crash_log.txt", ex.ToString());
            }
        }
    }
}




