using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using static MonitorManager;

namespace MonitorUtils
{
    public class MonitorControl : DoubleBufferedPanel
    {
        protected override void WndProc(ref Message m)
        {
            const int WM_POWERBROADCAST = 0x0218;
            const int PBT_POWERSETTINGCHANGE = 0x8013;

            if (m.Msg == WM_POWERBROADCAST && m.WParam.ToInt32() == PBT_POWERSETTINGCHANGE)
            {
                POWERBROADCAST_SETTING pbs = Marshal.PtrToStructure<POWERBROADCAST_SETTING>(m.LParam);
                Guid guid = new("02731015-4510-4526-99e6-e5a17ebd1aea"); // GUID_MONITOR_POWER_ON

                if (pbs.PowerSetting == guid && pbs.Data == 1)
                {
                    MessageBox.Show("ВКЛ");
                    _ = MonitorPowerManager.RestoreAllBrightness(); // восстановим яркость
                }
            }

            base.WndProc(ref m);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct POWERBROADCAST_SETTING
        {
            public Guid PowerSetting;
            public int DataLength;
            public byte Data;
        }

        public MonitorManager.MonitorInfo Monitor { get; }
        public event Action<int> BrightnessChanged;

        private readonly BrightnessSlider slider;
        private readonly Label nameLabel;
        private readonly Label valueLabel;
        private readonly Timer brightnessTimer;
        private int pendingBrightness;
        public BrightnessSlider Slider => slider;

        public MonitorControl(
            MonitorManager.MonitorInfo monitor,
            string name,
            Color primaryColor,
            Color trackColor,
            Color thumbHoverColor,
            Color backColor,
            Color textColor,
            bool isDarkTheme)
        {
            Monitor = monitor;
            this.Height = 88;
            this.BackColor = backColor;
            this.Padding = new Padding(0, 0, 0, 10);

            nameLabel = new Label
            {
                Text = name,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = textColor,
                Left = 17,
                Top = 12,
                AutoSize = true
            };

            valueLabel = new Label
            {
                Text = $"{monitor.Current}%",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = primaryColor,
                Left = this.Width + 35,
                Top = 14,
                AutoSize = true
            };

            slider = new BrightnessSlider(isDarkTheme)
            {
                Minimum = (int)monitor.Min,
                Maximum = (int)monitor.Max,
                Value = (int)monitor.Current,
                Left = 5,
                Top = 20,
                Width = 268,
                Height = 50,
                PrimaryColor = primaryColor,
                TrackColor = trackColor,
                ThumbHoverColor = thumbHoverColor,
                BackColor = backColor,
                TextColor = textColor
            };

            // Таймер для применения яркости
            brightnessTimer = new Timer
            {
                Interval = 30 // Можно поэкспериментировать, 30-50 мс обычно идеально
            };
            brightnessTimer.Tick += (s, e) =>
            {
                brightnessTimer.Stop();
                Task.Run(() =>
                {
                    try
                    {
                        MonitorManager.SetBrightness(Monitor.Handle, (uint)pendingBrightness);
                    }
                    catch { /* возможно, монитор недоступен — не критично */ }
                });
            };

            slider.ValueChanged += (value) =>
            {
                valueLabel.Text = $"{value}%";

                pendingBrightness = value;
                brightnessTimer.Stop();
                brightnessTimer.Start(); // перезапускаем с нуля

                BrightnessChanged?.Invoke(value); // уведомим остальных
            };

            this.Controls.Add(nameLabel);
            this.Controls.Add(valueLabel);
            this.Controls.Add(slider);

            this.Paint += (s, e) =>
            {
                using (var shadowBrush = new LinearGradientBrush(
                    new Rectangle(0, this.Height - 5, this.Width, 5),
                    Color.FromArgb(isDarkTheme ? 80 : 20, 0, 0, 0),
                    Color.Transparent,
                    90f))
                {
                    e.Graphics.FillRectangle(shadowBrush, 0, this.Height - 5, this.Width, 5);
                }
            };
        }

        public void SetBrightnessSilent(int value)
        {
            slider.SetValueSilent(value);
            valueLabel.Text = $"{value}%";

            pendingBrightness = value;
            brightnessTimer.Stop();
            brightnessTimer.Start(); // применим яркость чуть позже
        }

        public async Task AnimateBrightness(MonitorControl control, int from, int to, int delayMs = 15)
        {
            int step = from < to ? 1 : -1;
            for (int i = from; step > 0 ? i <= to : i >= to; i += step)
            {
                control.SetBrightnessSilent(i);
                await Task.Delay(delayMs);
            }
        }
    }

    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            DoubleBuffered = true;
        }
    }

}
