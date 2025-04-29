using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using static MonitorManager;

namespace MonitorUtils
{
    public partial class Form1 : Form
    {
        #region Static
        // Получить Instance класса
        static volatile Form1 thisClass;
        static object SyncObject = new object();
        public static Form1 GetInstance
        {
            get
            {
                if (thisClass == null)
                    lock (SyncObject)
                    {
                        if (thisClass == null)
                            thisClass = new Form1();
                    }
                return thisClass;
            }
        }
        #endregion

        AppConfig config;
        private List<string> monitorNames = new List<string>();
        public List<MonitorInfo> monitors = new List<MonitorManager.MonitorInfo>();
        public List<MonitorControl> monitorControls = new List<MonitorControl>();
        DateTimeController dt;
        private Color PrimaryColor;
        private Color TrackColor;
        private Color ThumbHoverColor;
        private Color PanelBackColor;
        private Color TextColor;
        private bool IsDarkTheme;
        public string appPath = AppDomain.CurrentDomain.BaseDirectory;
        public bool shouldReload = true;
        public int loadedMonitors = 0;

        [DllImport("UXTheme.dll", SetLastError = true, EntryPoint = "#138")]
        public static extern bool ShouldSystemUseDarkMode();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        public Form1()
        {
            InitializeComponent();
            DetectSystemTheme();
            InitializeFormStyles();
            this.MouseClick += Form1_MouseClick;
            this.Activated += Form1_Shown;
        }

        public async Task<List<MonitorManager.MonitorInfo>> GetMonitorsAsync()
        {
            return await Task.Run(() => MonitorManager.GetMonitors());
        }

        void PositionWindowBottomRight()
        {
            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(
                screen.Right - this.Width - 8,
                screen.Bottom - this.Height - 8
            );
        }

        private void DetectSystemTheme()
        {
            try { IsDarkTheme = ShouldSystemUseDarkMode(); }
            catch { IsDarkTheme = false; }
            UpdateColors();
        }

        private void UpdateColors()
        {
            if (IsDarkTheme)
            {
                PrimaryColor = Color.FromArgb(0, 120, 215);
                TrackColor = Color.FromArgb(60, 60, 60);
                ThumbHoverColor = Color.FromArgb(0, 90, 180);
                PanelBackColor = Color.FromArgb(32, 32, 32);
                TextColor = Color.FromArgb(240, 240, 240);
                this.BackColor = Color.FromArgb(25, 25, 25);
            }
            else
            {
                PrimaryColor = Color.FromArgb(0, 120, 215);
                TrackColor = Color.FromArgb(225, 225, 225);
                ThumbHoverColor = Color.FromArgb(0, 90, 180);
                PanelBackColor = Color.FromArgb(231, 231, 231);
                TextColor = Color.FromArgb(64, 64, 64);
                this.BackColor = Color.White;
            }
        }

        private void InitializeFormStyles()
        {
            this.Padding = new Padding(20);
            this.Size = new Size(450, 350);
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint, true);
        }
        public async Task LoadMonitors()
        {
            foreach (var ctrl in monitorControls)
            {
                this.Controls.Remove(ctrl);
                ctrl.Dispose();
            }
            monitorControls.Clear();

            int y = 50;

            for (int i = 0; i < monitors.Count; i++)
            {
                if (loadedMonitors != monitors.Count)
                {
                    shouldReload = true;
                }

                var monitor = monitors[i];
                string name = monitorNames.Count > i ? monitorNames[i] : $"Monitor {i + 1}";

                var control = new MonitorControl(
                    monitor,
                    name,
                    PrimaryColor,
                    TrackColor,
                    ThumbHoverColor,
                    PanelBackColor,
                    TextColor,
                    IsDarkTheme)
                {
                    Location = new Point(10, y),
                    Width = 280
                };

                control.BrightnessChanged += (value) =>
                {
                    if (config.linkSliders)
                    {
                        foreach (var mc in monitorControls)
                        {
                            if (mc != control)
                            {
                                mc.SetBrightnessSilent(value);
                            }
                        }
                    }
                };

                this.Controls.Add(control);
                monitorControls.Add(control);
                y += control.Height + 3;

                if (config.useTimedSettings)
                {
                    control.SetBrightnessSilent(dt.GetBrightnessForCurrentTime()); // Применить яркость по времени суток
                }
                else
                {
                    control.SetBrightnessSilent(                             // Применить яркость из файла settings.json
                    config.GetMonitorBrightness(i) > (int)monitor.Max ?
                    (int)monitor.Max : config.GetMonitorBrightness(i) < (int)monitor.Min ?
                    (int)monitor.Min : config.GetMonitorBrightness(i));
                }
                TrayManager.GetInstance.UpdateTooltip();
            }

            loadedMonitors = monitors.Count;

            this.Height = Math.Min(y + 6, Screen.PrimaryScreen.WorkingArea.Height);
        }


        protected override void WndProc(ref Message m)
        {
            const int WM_SETTINGCHANGE = 0x001A;
            const int WM_THEMECHANGED = 0x031A;

            if (m.Msg == WM_SETTINGCHANGE || m.Msg == WM_THEMECHANGED)
            {
                DetectSystemTheme();
                this.Invalidate(true);
            }
            base.WndProc(ref m);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        public async Task PrepareFormAsync()
        {
            if (dt == null || dt.IsDisposed)
                dt = new DateTimeController();

            config = AppConfig.Load();

            monitorNames.Clear();
            monitors.Clear();
            this.Controls.Clear();
            monitorNames = await GetMonitorNamesFromEDID();
            monitors = await GetMonitorsAsync();

            await LoadMonitors();
            CreateSettingsButtons();
            this.Width = 300;
            MonitorPowerManager.RegisterControls(monitorControls);

            if (dt.backgroundLoopStarted) return;
            if (!dt.backgroundLoopStarted)
            {
                dt.backgroundLoopStarted = true;
                _ = StartBackgroundLoopAsync();
            }
        }


        async void Form1_Deactivate(object sender, EventArgs e)
        {
            config = AppConfig.Load();
            for (int i = 0; i < monitors.Count; i++)
            {
                try
                {
                    config.SaveMonitorBrightness(i, monitorControls[i].Slider.Value);
                }
                catch (Exception ex) { await PrepareFormAsync(); }
            }
            TrayManager.GetInstance.UpdateTooltip();
            this.Hide();
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                _ = MonitorPowerManager.TurnOffMonitorsAsync();
            }
        }

        async void Form1_Shown(object sender, EventArgs e)
        {
            if (shouldReload)
            {
                shouldReload = false;
                await PrepareFormAsync();
            }
        }

        void CreateSettingsButtons()
        {
            var timedSettingsButton = new Button
            {
                Text = "🕙",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.White,
                BackColor = config.useTimedSettings ? PrimaryColor : Color.Gray,
                FlatStyle = FlatStyle.Flat,
                Width = 50,
                Height = 28,
                Top = 12,
                Left = 72
            };

            timedSettingsButton.FlatAppearance.BorderSize = 0;
            timedSettingsButton.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    if (dt == null || dt.IsDisposed)
                        dt = new DateTimeController();
                    dt.Show();
                    dt.TopMost = true;

                    int currentBrightness = dt.GetBrightnessForCurrentTime();
                }
                if (e.Button == MouseButtons.Left)
                {
                    config.useTimedSettings = !config.useTimedSettings;
                    config.Save();
                    timedSettingsButton.BackColor = config.useTimedSettings ? PrimaryColor : Color.Gray;
                    if (config.useTimedSettings)
                    {
                        foreach (MonitorControl control in monitorControls)
                            control.SetBrightnessSilent(dt.GetBrightnessForCurrentTime());
                    }
                }
            };
            this.Controls.Add(timedSettingsButton);


            var syncButton = new Button
            {
                Text = "🔗",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.White,
                BackColor = Color.Gray,
                FlatStyle = FlatStyle.Flat,
                Width = 50,
                Height = 28,
                Top = 12,
                Left = 12
            };

            syncButton.FlatAppearance.BorderSize = 0;
            syncButton.Click += (s, e) =>
            {
                config.linkSliders = !config.linkSliders;
                syncButton.BackColor = config.linkSliders ? PrimaryColor : Color.Gray;
                config.Save();
            };
            syncButton.BackColor = config.linkSliders ? PrimaryColor : Color.Gray;
            this.Controls.Add(syncButton);
        }


        public async Task<List<string>> GetMonitorNamesFromEDID()
        {
            var names = new List<string>();
            var searcher = new ManagementObjectSearcher(@"ROOT\WMI", "SELECT * FROM WmiMonitorID");
            int index = 0;

            foreach (ManagementObject obj in searcher.Get())
            {
                index++;
                string name = GetString(obj["UserFriendlyName"]);
                if (!string.IsNullOrWhiteSpace(name))
                    names.Add(name + $" ({index})");
            }
            return names;
        }

        private string GetString(object value)
        {
            if (value is ushort[] chars)
            {
                return new string(chars.Select(c => (char)c).ToArray()).TrimEnd('\0');
            }
            return string.Empty;
        }

        private async Task StartBackgroundLoopAsync()
        {
            while (true)
            {
                if (dt.CheckBrightnessTime())
                {
                    foreach (var mc in monitorControls)
                    {
                        try
                        {
                            mc.SetBrightnessSilent(dt.GetBrightnessForCurrentTime());
                            TrayManager.GetInstance.UpdateTooltip();
                        }
                        catch { }
                    }
                }
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }
    }
}