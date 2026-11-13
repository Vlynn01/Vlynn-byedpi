using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("VlynnByeDPI GUI")]
[assembly: AssemblyDescription("Advanced DPI Bypass & Secure DNS Optimizer")]
[assembly: AssemblyProduct("VlynnByeDPI")]
[assembly: AssemblyCopyright("Copyright © Vlynn")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]
[assembly: ComVisible(false)]

namespace VlynnByeDPI
{
    internal static class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool DeleteFile(string lpFileName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);
    }

    public class MainForm : Form
    {
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private ToolStripMenuItem _menuStop;
        private Process _engineProcess;
        private readonly string _appDir;
        private readonly string _archDir;
        private readonly string _enginePath;
        private readonly string _logPath;
        private Icon _coloredIcon;
        private Icon _grayscaleIcon;

        private Panel _statusCard;
        private Label _statusLabel;
        private Label _subStatusLabel;

        private Font _titleFont;
        private Font _subtitleFont;
        private Font _statusFont;
        private Font _subStatusFont;
        private Font _buttonFont;
        private Font _buttonActiveFont;

        private Button _btnMode1;
        private Button _btnMode2;
        private Button _btnMode3;
        private Button _btnMode4;
        private Button _btnStop;
        private Button _btnSettings;

        private bool _isSystemDnsChanged = false;
        private bool _isRunning = false;
        private bool _isTransitioning = false;
        private int _activeModeIndex = 0;
        private int _operationId = 0;
        private readonly object _procLock = new object();
        private readonly StringBuilder _engineErrorBuffer = new StringBuilder();

        // Theme Colors
        private static readonly Color BgDark = Color.FromArgb(18, 20, 28);
        private static readonly Color CardBg = Color.FromArgb(26, 30, 42);
        private static readonly Color BtnNormalBg = Color.FromArgb(34, 39, 54);
        private static readonly Color BtnHoverBg = Color.FromArgb(46, 53, 72);
        private static readonly Color BtnBorder = Color.FromArgb(55, 63, 84);
        private static readonly Color StopBtnBg = Color.FromArgb(190, 42, 55);
        private static readonly Color StopBtnHover = Color.FromArgb(220, 55, 70);

        private const int FormWidth = 380;
        private const int FormHeightCollapsed = 355;
        private const int FormHeightExpanded = 410;

        public MainForm()
        {
            _appDir = AppDomain.CurrentDomain.BaseDirectory;
            _archDir = Path.Combine(_appDir, "x86_64");
            _enginePath = Path.Combine(_archDir, "vlynnengine.exe");
            _logPath = Path.Combine(_appDir, "error.log");

            EnsureEngineFiles();
            InitializeComponents();
            LoadIcons();
            UpdateStatus(false, "", 0, "Select a mode below to activate protection");

            string configPath = Path.Combine(_appDir, "config.ini");
            if (File.Exists(configPath))
            {
                try
                {
                    string configContent = File.ReadAllText(configPath);
                    if (configContent.Contains("AutoConnect=True"))
                    {
                        if (configContent.Contains("StartupMode=1")) StartMode1(null, EventArgs.Empty);
                        else if (configContent.Contains("StartupMode=2")) StartMode2(null, EventArgs.Empty);
                        else if (configContent.Contains("StartupMode=4")) StartMode4(null, EventArgs.Empty);
                        else StartMode3(null, EventArgs.Empty);
                    }
                }
                catch (Exception ex)
                {
                    LogError("Startup Config Error: " + ex.Message);
                }
            }
        }

        private void LogError(string message)
        {
            try
            {
                string line = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] {1}{2}", DateTime.Now, message, Environment.NewLine);
                File.AppendAllText(_logPath, line);
            }
            catch { }
        }

        private void UnblockFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    NativeMethods.DeleteFile(path + ":Zone.Identifier");
                }
            }
            catch { }
        }

        private bool EnsureEngineFiles()
        {
            try
            {
                if (!Directory.Exists(_archDir))
                {
                    Directory.CreateDirectory(_archDir);
                }

                string[] files = new string[] { "icon.ico", "vlynnengine.exe", "WinDivert.dll", "WinDivert64.sys" };
                Assembly asm = Assembly.GetExecutingAssembly();

                foreach (string fileName in files)
                {
                    string targetPath = Path.Combine(_archDir, fileName);
                    bool needsExtract = !File.Exists(targetPath);
                    if (!needsExtract)
                    {
                        try
                        {
                            FileInfo fi = new FileInfo(targetPath);
                            if (fi.Length == 0) needsExtract = true;
                        }
                        catch { needsExtract = true; }
                    }

                    if (needsExtract)
                    {
                        string resName = "VlynnByeDPI." + fileName;
                        using (Stream s = asm.GetManifestResourceStream(resName))
                        {
                            if (s != null)
                            {
                                using (FileStream fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                                {
                                    s.CopyTo(fs);
                                }
                            }
                        }
                    }

                    UnblockFile(targetPath);
                }

                UnblockFile(Process.GetCurrentProcess().MainModule.FileName);
                return File.Exists(_enginePath) &&
                       File.Exists(Path.Combine(_archDir, "WinDivert.dll")) &&
                       File.Exists(Path.Combine(_archDir, "WinDivert64.sys"));
            }
            catch (Exception ex)
            {
                LogError("EnsureEngineFiles Error: " + ex.ToString());
                return File.Exists(_enginePath);
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
            this.DoubleBuffered = true;
            this.Text = "VlynnByeDPI v1.1 - Control Panel";
            this.ClientSize = new Size(FormWidth, FormHeightCollapsed);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BgDark;
            this.ForeColor = Color.White;

            _titleFont = new Font("Segoe UI", 15f, FontStyle.Bold);
            _subtitleFont = new Font("Segoe UI", 8.25f, FontStyle.Regular);
            _statusFont = new Font("Segoe UI", 10f, FontStyle.Bold);
            _subStatusFont = new Font("Segoe UI", 8.25f, FontStyle.Regular);
            _buttonFont = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            _buttonActiveFont = new Font("Segoe UI", 9.25f, FontStyle.Bold);

            // Header Title
            Label titleLabel = new Label();
            titleLabel.Text = "VlynnByeDPI v1.1";
            titleLabel.Font = _titleFont;
            titleLabel.ForeColor = Color.White;
            titleLabel.AutoSize = false;
            titleLabel.Size = new Size(260, 28);
            titleLabel.Location = new Point(24, 14);
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(titleLabel);

            Label subHeaderLabel = new Label();
            subHeaderLabel.Text = "DPI Bypass & Secure DNS Engine";
            subHeaderLabel.Font = _subtitleFont;
            subHeaderLabel.ForeColor = Color.FromArgb(145, 155, 175);
            subHeaderLabel.AutoSize = false;
            subHeaderLabel.Size = new Size(260, 16);
            subHeaderLabel.Location = new Point(26, 42);
            subHeaderLabel.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(subHeaderLabel);

            // Settings Button
            _btnSettings = new Button();
            _btnSettings.Text = "⚙";
            _btnSettings.Font = new Font("Segoe UI", 12f, FontStyle.Regular);
            _btnSettings.Size = new Size(36, 36);
            _btnSettings.Location = new Point(FormWidth - 60, 18);
            _btnSettings.FlatStyle = FlatStyle.Flat;
            _btnSettings.FlatAppearance.BorderSize = 1;
            _btnSettings.FlatAppearance.BorderColor = BtnBorder;
            _btnSettings.FlatAppearance.MouseOverBackColor = BtnHoverBg;
            _btnSettings.BackColor = BtnNormalBg;
            _btnSettings.ForeColor = Color.White;
            _btnSettings.Cursor = Cursors.Hand;
            _btnSettings.Click += delegate(object s, EventArgs e)
            {
                using (SettingsForm sf = new SettingsForm(_appDir))
                {
                    sf.ShowDialog(this);
                }
            };
            this.Controls.Add(_btnSettings);

            // Status Card Panel
            _statusCard = new Panel();
            _statusCard.Location = new Point(24, 68);
            _statusCard.Size = new Size(FormWidth - 48, 56);
            _statusCard.BackColor = CardBg;
            _statusCard.Paint += StatusCard_Paint;

            _statusLabel = new Label();
            _statusLabel.Text = "● Status: OFF";
            _statusLabel.Font = _statusFont;
            _statusLabel.ForeColor = Color.FromArgb(150, 158, 175);
            _statusLabel.AutoSize = false;
            _statusLabel.Size = new Size(_statusCard.Width - 20, 24);
            _statusLabel.Location = new Point(10, 7);
            _statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            _statusCard.Controls.Add(_statusLabel);

            _subStatusLabel = new Label();
            _subStatusLabel.Text = "Select a mode below to activate protection";
            _subStatusLabel.Font = _subStatusFont;
            _subStatusLabel.ForeColor = Color.FromArgb(130, 138, 155);
            _subStatusLabel.AutoSize = false;
            _subStatusLabel.Size = new Size(_statusCard.Width - 16, 18);
            _subStatusLabel.Location = new Point(8, 31);
            _subStatusLabel.TextAlign = ContentAlignment.MiddleCenter;
            _statusCard.Controls.Add(_subStatusLabel);

            this.Controls.Add(_statusCard);

            // Mode Buttons
            _btnMode1 = CreateModeButton("1. AdBlock DNS & Bypass (DoH)", 138, StartMode1);
            _btnMode2 = CreateModeButton("2. Unfiltered DNS (Max Compatibility)", 188, StartMode2);
            _btnMode3 = CreateModeButton("3. Gaming & Safe DNS (Recommended)", 238, StartMode3);
            _btnMode4 = CreateModeButton("4. Custom DNS (Power User)", 288, StartMode4);

            // Stop Button (Hidden by default when Status is OFF)
            _btnStop = new Button();
            _btnStop.Text = "■  STOP PROTECTION";
            _btnStop.Location = new Point(24, 348);
            _btnStop.Size = new Size(FormWidth - 48, 40);
            _btnStop.FlatStyle = FlatStyle.Flat;
            _btnStop.FlatAppearance.BorderSize = 1;
            _btnStop.FlatAppearance.BorderColor = Color.FromArgb(235, 80, 90);
            _btnStop.FlatAppearance.MouseOverBackColor = StopBtnHover;
            _btnStop.BackColor = StopBtnBg;
            _btnStop.ForeColor = Color.White;
            _btnStop.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            _btnStop.Cursor = Cursors.Hand;
            _btnStop.Visible = false;
            _btnStop.Enabled = false;
            _btnStop.Click += StopEngineClick;
            this.Controls.Add(_btnStop);

            // System Tray Menu
            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.Add("Show Control Panel", null, delegate(object s, EventArgs e) { ShowForm(); });
            _trayMenu.Items.Add("-");
            _trayMenu.Items.Add("Mode 1: AdBlock DNS & Bypass", null, delegate(object s, EventArgs e) { StartMode1(s, e); });
            _trayMenu.Items.Add("Mode 2: Unfiltered DNS", null, delegate(object s, EventArgs e) { StartMode2(s, e); });
            _trayMenu.Items.Add("Mode 3: Gaming & Safe DNS", null, delegate(object s, EventArgs e) { StartMode3(s, e); });
            _trayMenu.Items.Add("Mode 4: Custom DNS", null, delegate(object s, EventArgs e) { StartMode4(s, e); });
            _trayMenu.Items.Add("-");

            _menuStop = new ToolStripMenuItem("Stop Protection", null, delegate(object s, EventArgs e) { StopEngineClick(s, e); });
            _menuStop.Enabled = false;
            _trayMenu.Items.Add(_menuStop);

            _trayMenu.Items.Add("-");
            _trayMenu.Items.Add("Exit completely", null, OnExit);

            _trayIcon = new NotifyIcon();
            _trayIcon.Text = "VlynnByeDPI (OFF)";
            _trayIcon.ContextMenuStrip = _trayMenu;
            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += delegate(object s, EventArgs e) { ShowForm(); };

            this.FormClosing += MainForm_FormClosing;
        }

        private void StatusCard_Paint(object sender, PaintEventArgs e)
        {
            Color borderColor = BtnBorder;
            if (_isTransitioning) borderColor = Color.FromArgb(230, 150, 40);
            else if (_isRunning)
            {
                if (_activeModeIndex == 1) borderColor = Color.FromArgb(46, 204, 113);
                else if (_activeModeIndex == 2) borderColor = Color.FromArgb(255, 120, 70);
                else if (_activeModeIndex == 3) borderColor = Color.FromArgb(0, 200, 255);
                else if (_activeModeIndex == 4) borderColor = Color.FromArgb(235, 90, 175);
            }

            using (Pen pen = new Pen(borderColor, 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, _statusCard.Width - 1, _statusCard.Height - 1);
            }
        }

        private Button CreateModeButton(string text, int y, EventHandler onClick)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Tag = text;
            btn.Location = new Point(24, y);
            btn.Size = new Size(FormWidth - 48, 40);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = BtnBorder;
            btn.FlatAppearance.MouseOverBackColor = BtnHoverBg;
            btn.BackColor = BtnNormalBg;
            btn.ForeColor = Color.FromArgb(235, 240, 250);
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
                    if (_grayscaleIcon != null) _trayIcon.Icon = _grayscaleIcon;
                    else _trayIcon.Icon = _coloredIcon;
                }
                catch (Exception ex)
                {
                    LogError("LoadIcons Error: " + ex.ToString());
                }
            }
        }

        private Icon MakeGrayscaleIcon(Bitmap original)
        {
            IntPtr hIcon = IntPtr.Zero;
            try
            {
                using (Bitmap gray = new Bitmap(original.Width, original.Height))
                {
                    using (Graphics g = Graphics.FromImage(gray))
                    {
                        ColorMatrix colorMatrix = new ColorMatrix(new float[][]
                        {
                            new float[] {0.3f, 0.3f, 0.3f, 0, 0},
                            new float[] {0.59f, 0.59f, 0.59f, 0, 0},
                            new float[] {0.11f, 0.11f, 0.11f, 0, 0},
                            new float[] {0, 0, 0, 1, 0},
                            new float[] {0, 0, 0, 0, 1}
                        });

                        using (ImageAttributes attributes = new ImageAttributes())
                        {
                            attributes.SetColorMatrix(colorMatrix);
                            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                                0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
                        }
                    }

                    hIcon = gray.GetHicon();
                    using (Icon tempIcon = Icon.FromHandle(hIcon))
                    {
                        return (Icon)tempIcon.Clone();
                    }
                }
            }
            finally
            {
                if (hIcon != IntPtr.Zero)
                {
                    NativeMethods.DestroyIcon(hIcon);
                }
            }
        }

        private void SetControlsInteractable(bool interactable)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<bool>(SetControlsInteractable), interactable);
                return;
            }

            _btnMode1.Enabled = interactable;
            _btnMode2.Enabled = interactable;
            _btnMode3.Enabled = interactable;
            _btnMode4.Enabled = interactable;
            _btnSettings.Enabled = interactable;

            // Stop button is ONLY enabled and visible when system is actually running and not transitioning
            bool canStop = interactable && _isRunning;
            _btnStop.Enabled = canStop;
            _btnStop.Visible = _isRunning;
            if (_menuStop != null) _menuStop.Enabled = canStop;
        }

        private void RunAsyncOperation(string progressTitle, string progressDetail, Action<int> work)
        {
            int opId = Interlocked.Increment(ref _operationId);
            _isTransitioning = true;
            SetControlsInteractable(false);

            UpdateTransitionUI(progressTitle, progressDetail);

            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                try
                {
                    work(opId);
                }
                catch (Exception ex)
                {
                    LogError("AsyncOperation Error: " + ex.ToString());
                    if (opId == _operationId)
                    {
                        UpdateStatus(false, "", 0, "Error: " + ex.Message, true);
                    }
                }
                finally
                {
                    if (opId == _operationId)
                    {
                        _isTransitioning = false;
                        SetControlsInteractable(true);
                        if (this.IsHandleCreated)
                        {
                            this.BeginInvoke(new Action(delegate() { _statusCard.Invalidate(); }));
                        }
                    }
                }
            });
        }

        private void UpdateTransitionUI(string title, string detail)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<string, string>(UpdateTransitionUI), title, detail);
                return;
            }

            _statusLabel.Text = title;
            _statusLabel.ForeColor = Color.FromArgb(255, 180, 50);
            _subStatusLabel.Text = detail;
            _subStatusLabel.ForeColor = Color.FromArgb(200, 175, 130);
            _statusCard.Invalidate();
            if (_trayIcon != null) _trayIcon.Text = "VlynnByeDPI (Working...)";
        }

        private void StopEngineClick(object sender, EventArgs e)
        {
            if (!_isRunning) return;
            RunAsyncOperation("⏳ Stopping Protection...", "Restoring network & stopping engine...", delegate(int opId)
            {
                StopEngineInternal();
                if (opId == _operationId)
                {
                    UpdateStatus(false, "", 0, "Protection stopped. Select a mode to start.");
                }
            });
        }

        private void StartMode1(object sender, EventArgs e)
        {
            RunAsyncOperation("⚡ Activating Mode 1...", "Configuring AdGuard DoH & DPI Bypass...", delegate(int opId)
            {
                StopEngineInternal();

                bool dnsSuccess = ApplyAdGuardDns();
                string errMsg;
                // Also start DPI Bypass (-6 without overriding Windows DoH DNS)
                bool engineSuccess = TryStartEngineProcess("-6", out errMsg);

                if (opId != _operationId) return;

                if (dnsSuccess || engineSuccess)
                {
                    string detail = (dnsSuccess && engineSuccess)
                        ? "AdGuard DoH + DPI Bypass Active"
                        : (dnsSuccess ? "AdGuard DoH Active (System DNS)" : "DPI Bypass Active");
                    UpdateStatus(true, "Mode 1 (AdBlock DoH)", 1, detail);
                }
                else
                {
                    UpdateStatus(false, "", 0, "Failed to start Mode 1: " + errMsg, true);
                }
            });
        }

        private void StartMode2(object sender, EventArgs e)
        {
            RunAsyncOperation("⚡ Activating Mode 2...", "Starting Unfiltered DNS & Max Bypass...", delegate(int opId)
            {
                ActivateEngineMode(
                    opId,
                    2,
                    "Mode 2 (Unfiltered)",
                    "Yandex Unfiltered DNS (Port 1253) + IPv6",
                    "-6 --dns-addr 77.88.8.8 --dns-port 1253 --dnsv6-addr 2a02:6b8::feed:0ff --dnsv6-port 1253",
                    "-6 --dns-addr 77.88.8.8 --dns-port 1253"
                );
            });
        }

        private void StartMode3(object sender, EventArgs e)
        {
            RunAsyncOperation("⚡ Activating Mode 3...", "Starting Gaming & Safe DNS Engine...", delegate(int opId)
            {
                ActivateEngineMode(
                    opId,
                    3,
                    "Mode 3 (Gaming & Safe)",
                    "Low-Latency Gaming DNS + Anti-Leak Active",
                    "-6 --dns-addr 77.88.8.1 --dns-port 1253 --dnsv6-addr 2a02:6b8::feed:bad --dnsv6-port 1253",
                    "-6 --dns-addr 77.88.8.1 --dns-port 1253"
                );
            });
        }

        private void StartMode4(object sender, EventArgs e)
        {
            string lastDnsFile = Path.Combine(_appDir, "last_dns.txt");
            string selectedIp4 = null;

            if (Program.StartMinimized && sender == null)
            {
                selectedIp4 = File.Exists(lastDnsFile) ? File.ReadAllText(lastDnsFile).Trim() : "1.1.1.1";
            }
            else
            {
                using (CustomDnsForm dlg = new CustomDnsForm(lastDnsFile))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        selectedIp4 = dlg.SelectedIPv4;
                    }
                }
            }

            if (string.IsNullOrEmpty(selectedIp4)) return;

            string ip6;
            int port;
            string providerName;
            ResolveCustomDnsProfile(selectedIp4, out ip6, out port, out providerName);

            try { File.WriteAllText(lastDnsFile, selectedIp4); } catch { }

            string primaryArgs = string.Format("-6 --dns-addr {0} --dns-port {1} --dnsv6-addr {2} --dnsv6-port {1}", selectedIp4, port, ip6);
            string fallbackArgs = string.Format("-6 --dns-addr {0} --dns-port {1}", selectedIp4, port);

            RunAsyncOperation("⚡ Activating Mode 4...", "Routing via " + providerName + " (" + selectedIp4 + ")...", delegate(int opId)
            {
                ActivateEngineMode(
                    opId,
                    4,
                    "Mode 4 (" + providerName + ")",
                    string.Format("Custom DNS: {0} (Port {1}) + IPv6", selectedIp4, port),
                    primaryArgs,
                    fallbackArgs
                );
            });
        }

        private void ResolveCustomDnsProfile(string ip4, out string ip6, out int port, out string providerName)
        {
            if (ip4 == "8.8.8.8" || ip4 == "8.8.4.4")
            {
                ip6 = "2001:4860:4860::8888";
                port = 53;
                providerName = "Google DNS";
            }
            else if (ip4 == "9.9.9.9" || ip4 == "149.112.112.112")
            {
                ip6 = "2620:fe::fe";
                port = 53;
                providerName = "Quad9 DNS";
            }
            else if (ip4 == "208.67.222.222" || ip4 == "208.67.220.220")
            {
                ip6 = "2620:119:35::35";
                port = 5353;
                providerName = "OpenDNS";
            }
            else if (ip4 == "94.140.14.14" || ip4 == "94.140.15.15")
            {
                ip6 = "2a10:50c0::ad1:ff";
                port = 53;
                providerName = "AdGuard DNS";
            }
            else
            {
                ip6 = "2606:4700:4700::1111";
                port = 53;
                providerName = "Cloudflare";
            }
        }

        private void ActivateEngineMode(int opId, int modeIndex, string modeTitle, string detailText, string primaryArgs, string fallbackArgs)
        {
            StopEngineInternal();

            string errMsg;
            bool ok = TryStartEngineProcess(primaryArgs, out errMsg);

            if (!ok && !string.IsNullOrEmpty(fallbackArgs) && fallbackArgs != primaryArgs)
            {
                LogError("Primary args failed (" + errMsg + "), retrying with IPv4 fallback: " + fallbackArgs);
                ok = TryStartEngineProcess(fallbackArgs, out errMsg);
                if (ok)
                {
                    detailText += " (IPv4 Mode)";
                }
            }

            if (opId != _operationId) return;

            if (ok)
            {
                UpdateStatus(true, modeTitle, modeIndex, detailText);
            }
            else
            {
                UpdateStatus(false, "", 0, "Failed to start: " + errMsg, true);
            }
        }

        private bool ApplyAdGuardDns()
        {
            try
            {
                string psCmd =
                    "$ErrorActionPreference = 'SilentlyContinue'; " +
                    "if (Get-Command Add-DnsClientDoHServerAddress -ErrorAction SilentlyContinue) { " +
                    "  Add-DnsClientDoHServerAddress -ServerAddress '94.140.14.14' -DoHTemplate 'https://dns.adguard-dns.com/dns-query' -AllowFallbackToUdp $True; " +
                    "  Add-DnsClientDoHServerAddress -ServerAddress '94.140.15.15' -DoHTemplate 'https://dns.adguard-dns.com/dns-query' -AllowFallbackToUdp $True; " +
                    "  Add-DnsClientDoHServerAddress -ServerAddress '2a10:50c0::ad1:ff' -DoHTemplate 'https://dns.adguard-dns.com/dns-query' -AllowFallbackToUdp $True; " +
                    "  Add-DnsClientDoHServerAddress -ServerAddress '2a10:50c0::ad2:ff' -DoHTemplate 'https://dns.adguard-dns.com/dns-query' -AllowFallbackToUdp $True; " +
                    "} " +
                    "$adapters = @(Get-NetAdapter | Where-Object { $_.Status -eq 'Up' }); " +
                    "if ($adapters.Count -eq 0) { exit 2 } " +
                    "foreach ($a in $adapters) { " +
                    "  Set-DnsClientServerAddress -InterfaceIndex $a.InterfaceIndex -ServerAddresses @('94.140.14.14','94.140.15.15','2a10:50c0::ad1:ff','2a10:50c0::ad2:ff'); " +
                    "  if ($?) { $ok = $true } else { " +
                    "    Set-DnsClientServerAddress -InterfaceIndex $a.InterfaceIndex -ServerAddresses @('94.140.14.14','94.140.15.15'); " +
                    "    if ($?) { $ok = $true } " +
                    "  } " +
                    "} " +
                    "ipconfig /flushdns | Out-Null; " +
                    "if ($ok) { exit 0 } else { exit 1 }";

                ProcessStartInfo psi = new ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -WindowStyle Hidden -Command \"" + psCmd + "\"");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                using (Process p = Process.Start(psi))
                {
                    if (p != null)
                    {
                        p.WaitForExit(8000);
                        if (p.HasExited && p.ExitCode == 0)
                        {
                            _isSystemDnsChanged = true;
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("DNS Change Error: " + ex.ToString());
            }
            return false;
        }

        private bool TryStartEngineProcess(string args, out string errorMsg)
        {
            errorMsg = "";
            if (!EnsureEngineFiles())
            {
                errorMsg = "Engine files missing in x86_64 (check Antivirus).";
                LogError(errorMsg);
                return false;
            }

            try
            {
                lock (_procLock)
                {
                    _engineErrorBuffer.Length = 0;

                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = _enginePath;
                    psi.Arguments = args;
                    psi.WorkingDirectory = _archDir;
                    psi.WindowStyle = ProcessWindowStyle.Hidden;
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    psi.RedirectStandardOutput = true;
                    psi.RedirectStandardError = true;

                    Process proc = new Process();
                    proc.StartInfo = psi;
                    proc.EnableRaisingEvents = true;
                    proc.OutputDataReceived += EngineOutputHandler;
                    proc.ErrorDataReceived += EngineOutputHandler;
                    proc.Exited += OnEngineProcessExited;

                    if (!proc.Start())
                    {
                        errorMsg = "Could not start vlynnengine.exe.";
                        proc.Dispose();
                        return false;
                    }

                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    // Verify process remains alive for 350ms (detects immediate WinDivert / filter / DLL failure)
                    bool exitedQuickly = proc.WaitForExit(350);
                    if (exitedQuickly || proc.HasExited)
                    {
                        int exitCode = proc.ExitCode;
                        string captured = _engineErrorBuffer.ToString().Trim();
                        proc.Exited -= OnEngineProcessExited;
                        proc.Dispose();

                        errorMsg = !string.IsNullOrEmpty(captured)
                            ? captured
                            : string.Format("Engine exited immediately (Code {0}). Check WinDivert/Antivirus.", exitCode);
                        LogError("Engine failed to stay alive: " + errorMsg);
                        return false;
                    }

                    _engineProcess = proc;
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
                LogError("StartEngine Exception: " + ex.ToString());
                return false;
            }
        }

        private void EngineOutputHandler(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                lock (_procLock)
                {
                    if (_engineErrorBuffer.Length < 512)
                    {
                        if (_engineErrorBuffer.Length > 0) _engineErrorBuffer.Append(" | ");
                        _engineErrorBuffer.Append(e.Data.Trim());
                    }
                }
            }
        }

        private void OnEngineProcessExited(object sender, EventArgs e)
        {
            Process exitedProc = sender as Process;
            bool shouldResetUi = false;

            lock (_procLock)
            {
                if (_engineProcess != null && ReferenceEquals(_engineProcess, exitedProc))
                {
                    _engineProcess.Dispose();
                    _engineProcess = null;

                    // If Mode 1 is running with system DNS active, keep Mode 1 active; otherwise mark stopped
                    if (!_isSystemDnsChanged && _isRunning)
                    {
                        shouldResetUi = true;
                    }
                }
            }

            if (shouldResetUi)
            {
                LogError("vlynnengine.exe exited unexpectedly.");
                UpdateStatus(false, "", 0, "Engine stopped unexpectedly. Click a mode to restart.", true);
            }
        }

        private void StopEngineInternal()
        {
            if (_isSystemDnsChanged)
            {
                try
                {
                    string resetCmd = "Get-NetAdapter | Where-Object Status -eq 'Up' | Set-DnsClientServerAddress -ResetServerAddresses -ErrorAction SilentlyContinue; ipconfig /flushdns | Out-Null";
                    ProcessStartInfo psi = new ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -WindowStyle Hidden -Command \"" + resetCmd + "\"");
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    using (Process p = Process.Start(psi))
                    {
                        if (p != null) p.WaitForExit(5000);
                    }
                }
                catch (Exception ex)
                {
                    LogError("DNS Reset Error: " + ex.ToString());
                }
                _isSystemDnsChanged = false;
            }

            lock (_procLock)
            {
                if (_engineProcess != null)
                {
                    try
                    {
                        _engineProcess.Exited -= OnEngineProcessExited;
                        if (!_engineProcess.HasExited)
                        {
                            _engineProcess.Kill();
                            _engineProcess.WaitForExit(1500);
                        }
                    }
                    catch { }
                    finally
                    {
                        _engineProcess.Dispose();
                        _engineProcess = null;
                    }
                }
            }

            try
            {
                Process[] procs = Process.GetProcessesByName("vlynnengine");
                foreach (Process p in procs)
                {
                    try
                    {
                        if (!p.HasExited)
                        {
                            p.Kill();
                            p.WaitForExit(1500);
                        }
                    }
                    catch { }
                    finally
                    {
                        p.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Kill lingering processes error: " + ex.ToString());
            }
        }

        private void UpdateStatus(bool isRunning, string modeName, int modeIndex, string subStatusText = "", bool isError = false)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<bool, string, int, string, bool>(UpdateStatus), isRunning, modeName, modeIndex, subStatusText, isError);
                return;
            }

            _isRunning = isRunning;
            _activeModeIndex = isRunning ? modeIndex : 0;

            Color accentColor = Color.FromArgb(150, 158, 175);
            if (isRunning)
            {
                if (modeIndex == 1) accentColor = Color.FromArgb(46, 204, 113);
                else if (modeIndex == 2) accentColor = Color.FromArgb(255, 120, 70);
                else if (modeIndex == 3) accentColor = Color.FromArgb(0, 205, 255);
                else if (modeIndex == 4) accentColor = Color.FromArgb(240, 100, 185);
                else accentColor = Color.LimeGreen;

                _statusLabel.Text = "● RUNNING — " + modeName;
                _statusLabel.ForeColor = accentColor;
                _subStatusLabel.Text = string.IsNullOrEmpty(subStatusText) ? "DPI Bypass & DNS Protection Active" : subStatusText;
                _subStatusLabel.ForeColor = Color.FromArgb(190, 200, 215);

                if (_coloredIcon != null) _trayIcon.Icon = _coloredIcon;
                string trayText = "VlynnByeDPI (ON) - " + modeName;
                _trayIcon.Text = trayText.Length > 63 ? trayText.Substring(0, 63) : trayText;
            }
            else
            {
                if (isError)
                {
                    _statusLabel.Text = "⚠ FAILED TO START";
                    _statusLabel.ForeColor = Color.FromArgb(255, 95, 95);
                    _subStatusLabel.Text = string.IsNullOrEmpty(subStatusText) ? "Could not start selected mode." : subStatusText;
                    _subStatusLabel.ForeColor = Color.FromArgb(235, 140, 140);
                }
                else
                {
                    _statusLabel.Text = "● Status: OFF";
                    _statusLabel.ForeColor = Color.FromArgb(150, 158, 175);
                    _subStatusLabel.Text = string.IsNullOrEmpty(subStatusText) ? "Select a mode below to activate protection" : subStatusText;
                    _subStatusLabel.ForeColor = Color.FromArgb(130, 138, 155);
                }

                if (_grayscaleIcon != null) _trayIcon.Icon = _grayscaleIcon;
                else if (_coloredIcon != null) _trayIcon.Icon = _coloredIcon;
                _trayIcon.Text = "VlynnByeDPI (OFF)";
            }

            HighlightActiveModeButton(_activeModeIndex, accentColor);

            // Show and enable STOP button ONLY when running
            _btnStop.Visible = _isRunning;
            _btnStop.Enabled = _isRunning && !_isTransitioning;
            if (_menuStop != null) _menuStop.Enabled = _isRunning && !_isTransitioning;

            int targetHeight = _isRunning ? FormHeightExpanded : FormHeightCollapsed;
            if (this.ClientSize.Height != targetHeight)
            {
                this.ClientSize = new Size(FormWidth, targetHeight);
            }

            foreach (ToolStripItem item in _trayMenu.Items)
            {
                ToolStripMenuItem menuItem = item as ToolStripMenuItem;
                if (menuItem != null && menuItem.Text.StartsWith("Mode "))
                {
                    menuItem.Checked = _isRunning && menuItem.Text.StartsWith("Mode " + _activeModeIndex + ":");
                }
            }

            _statusCard.Invalidate();
        }

        private void HighlightActiveModeButton(int activeIndex, Color accentColor)
        {
            Button[] buttons = new Button[] { _btnMode1, _btnMode2, _btnMode3, _btnMode4 };
            for (int i = 0; i < buttons.Length; i++)
            {
                Button btn = buttons[i];
                string baseText = btn.Tag as string;
                bool isActive = (i + 1) == activeIndex;

                if (isActive)
                {
                    btn.Text = "✓  " + baseText;
                    btn.Font = _buttonActiveFont;
                    btn.BackColor = Color.FromArgb(30, 48, 62);
                    btn.ForeColor = accentColor;
                    btn.FlatAppearance.BorderColor = accentColor;
                }
                else
                {
                    btn.Text = baseText;
                    btn.Font = _buttonFont;
                    btn.BackColor = BtnNormalBg;
                    btn.ForeColor = Color.FromArgb(235, 240, 250);
                    btn.FlatAppearance.BorderColor = BtnBorder;
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
                _trayIcon.ShowBalloonTip(1000, "VlynnByeDPI", "Application is minimized to the system tray.", ToolTipIcon.Info);
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            Interlocked.Increment(ref _operationId);
            StopEngineInternal();
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
                if (_subtitleFont != null) _subtitleFont.Dispose();
                if (_statusFont != null) _statusFont.Dispose();
                if (_subStatusFont != null) _subStatusFont.Dispose();
                if (_buttonFont != null) _buttonFont.Dispose();
                if (_buttonActiveFont != null) _buttonActiveFont.Dispose();
                if (_btnSettings != null && _btnSettings.Font != null) _btnSettings.Font.Dispose();
                if (_btnStop != null && _btnStop.Font != null) _btnStop.Font.Dispose();
                if (_engineProcess != null) _engineProcess.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class CustomDnsForm : Form
    {
        public string SelectedIPv4 { get; private set; }
        private ComboBox _comboBox;

        public CustomDnsForm(string lastDnsFile)
        {
            this.Text = "Mode 4 — Custom DNS";
            this.ClientSize = new Size(360, 155);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(18, 20, 28);
            this.ForeColor = Color.White;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label titleLabel = new Label();
            titleLabel.Left = 20;
            titleLabel.Top = 18;
            titleLabel.Width = 320;
            titleLabel.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            titleLabel.Text = "Select Global Secure DNS Provider:";
            this.Controls.Add(titleLabel);

            _comboBox = new ComboBox();
            _comboBox.Left = 20;
            _comboBox.Top = 48;
            _comboBox.Width = 320;
            _comboBox.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            _comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _comboBox.BackColor = Color.FromArgb(34, 39, 54);
            _comboBox.ForeColor = Color.White;
            _comboBox.Items.AddRange(new string[]
            {
                "1.1.1.1 — Cloudflare DNS (Fastest, Port 53)",
                "8.8.8.8 — Google Public DNS (Reliable, Port 53)",
                "9.9.9.9 — Quad9 Security DNS (Malware Block, Port 53)",
                "208.67.222.222 — OpenDNS (Non-standard Port 5353)",
                "94.140.14.14 — AdGuard DNS (AdBlock, Port 53)"
            });

            if (File.Exists(lastDnsFile))
            {
                try
                {
                    string last = File.ReadAllText(lastDnsFile).Trim();
                    int idx = _comboBox.FindString(last);
                    _comboBox.SelectedIndex = idx >= 0 ? idx : 0;
                }
                catch { _comboBox.SelectedIndex = 0; }
            }
            else
            {
                _comboBox.SelectedIndex = 0;
            }
            this.Controls.Add(_comboBox);

            Button btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Left = 165;
            btnCancel.Top = 98;
            btnCancel.Size = new Size(80, 34);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(55, 63, 84);
            btnCancel.BackColor = Color.FromArgb(34, 39, 54);
            btnCancel.ForeColor = Color.White;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.DialogResult = DialogResult.Cancel;
            this.Controls.Add(btnCancel);

            Button btnConnect = new Button();
            btnConnect.Text = "Connect";
            btnConnect.Left = 255;
            btnConnect.Top = 98;
            btnConnect.Size = new Size(85, 34);
            btnConnect.FlatStyle = FlatStyle.Flat;
            btnConnect.FlatAppearance.BorderColor = Color.FromArgb(240, 100, 185);
            btnConnect.BackColor = Color.FromArgb(65, 35, 65);
            btnConnect.ForeColor = Color.White;
            btnConnect.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnConnect.Cursor = Cursors.Hand;
            btnConnect.Click += delegate(object s, EventArgs e)
            {
                if (_comboBox.SelectedItem != null)
                {
                    string selectedText = _comboBox.SelectedItem.ToString();
                    SelectedIPv4 = selectedText.Split(' ')[0].Trim();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            };
            this.Controls.Add(btnConnect);

            this.AcceptButton = btnConnect;
            this.CancelButton = btnCancel;
        }
    }

    public class SettingsForm : Form
    {
        private readonly string _appDir;
        private readonly string _exePath;
        private CheckBox _chkStartup;

        public SettingsForm(string appDir)
        {
            this._appDir = appDir;
            this._exePath = Process.GetCurrentProcess().MainModule.FileName;

            this.Text = "VlynnByeDPI — Settings";
            this.ClientSize = new Size(340, 190);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(18, 20, 28);
            this.ForeColor = Color.White;

            _chkStartup = new CheckBox();
            _chkStartup.Text = "Start with Windows (Minimized to Tray)";
            _chkStartup.Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            _chkStartup.Location = new Point(24, 24);
            _chkStartup.AutoSize = true;
            _chkStartup.Checked = CheckStartupTask();
            _chkStartup.CheckedChanged += ChkStartup_CheckedChanged;
            this.Controls.Add(_chkStartup);

            CheckBox chkAutoConnect = new CheckBox();
            chkAutoConnect.Text = "Auto-connect on startup:";
            chkAutoConnect.Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            chkAutoConnect.Location = new Point(24, 62);
            chkAutoConnect.AutoSize = true;

            ComboBox cmbStartupMode = new ComboBox();
            cmbStartupMode.Items.AddRange(new string[] { "Mode 1", "Mode 2", "Mode 3", "Mode 4" });
            cmbStartupMode.Location = new Point(210, 60);
            cmbStartupMode.Width = 105;
            cmbStartupMode.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            cmbStartupMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStartupMode.BackColor = Color.FromArgb(34, 39, 54);
            cmbStartupMode.ForeColor = Color.White;

            string configPath = Path.Combine(_appDir, "config.ini");
            string configContent = File.Exists(configPath) ? File.ReadAllText(configPath) : "";
            chkAutoConnect.Checked = configContent.Contains("AutoConnect=True");

            if (configContent.Contains("StartupMode=1")) cmbStartupMode.SelectedIndex = 0;
            else if (configContent.Contains("StartupMode=2")) cmbStartupMode.SelectedIndex = 1;
            else if (configContent.Contains("StartupMode=4")) cmbStartupMode.SelectedIndex = 3;
            else cmbStartupMode.SelectedIndex = 2; // Default Mode 3

            Action saveConfig = delegate()
            {
                try
                {
                    File.WriteAllText(configPath,
                        "AutoConnect=" + chkAutoConnect.Checked.ToString() + Environment.NewLine +
                        "StartupMode=" + (cmbStartupMode.SelectedIndex + 1).ToString());
                }
                catch { }
            };

            chkAutoConnect.CheckedChanged += delegate(object s, EventArgs e) { saveConfig(); };
            cmbStartupMode.SelectedIndexChanged += delegate(object s, EventArgs e) { saveConfig(); };

            this.Controls.Add(chkAutoConnect);
            this.Controls.Add(cmbStartupMode);

            Button btnClose = new Button();
            btnClose.Text = "Done";
            btnClose.Location = new Point(125, 130);
            btnClose.Size = new Size(90, 34);
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(55, 63, 84);
            btnClose.BackColor = Color.FromArgb(34, 39, 54);
            btnClose.ForeColor = Color.White;
            btnClose.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += delegate(object s, EventArgs e) { this.Close(); };
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
                    p.StartInfo.RedirectStandardError = true;
                    p.Start();
                    string outStr = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(3000);
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
                        p.WaitForExit(4000);
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
                        p.WaitForExit(4000);
                    }
                }
            }
            catch
            {
                _chkStartup.Checked = !_chkStartup.Checked;
            }
        }
    }

    static class Program
    {
        public static bool StartMinimized = false;
        private static Mutex _mutex;

        [STAThread]
        static void Main(string[] args)
        {
            bool createdNew;
            _mutex = new Mutex(true, "Local\\VlynnByeDPI_GUI_SingleInstance", out createdNew);
            if (!createdNew)
            {
                MessageBox.Show("VlynnByeDPI is already running (check your System Tray).", "VlynnByeDPI", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

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
                try
                {
                    string crashPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");
                    File.WriteAllText(crashPath, ex.ToString());
                }
                catch { }
            }
            finally
            {
                if (_mutex != null)
                {
                    _mutex.ReleaseMutex();
                    _mutex.Dispose();
                }
            }
        }
    }
}
