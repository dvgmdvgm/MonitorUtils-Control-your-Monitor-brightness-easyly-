using System;
using System.Drawing;
using System.Windows.Forms;

namespace MonitorUtils
{
    internal class DateTimeController : Form
    {
        #region Static
        // Получить Instance класса
        static volatile DateTimeController thisClass;
        static object SyncObject = new object();
        public static DateTimeController GetInstance
        {
            get
            {
                if (thisClass == null)
                    lock (SyncObject)
                    {
                        if (thisClass == null)
                            thisClass = new DateTimeController();
                    }
                return thisClass;
            }
        }
        #endregion

        private readonly NumericUpDown[] brightnessInputs = new NumericUpDown[6];
        private readonly Label[] timeLabels = new Label[6];
        private readonly Button saveButton;
        private readonly string[] timeKeys = { "00:00", "04:00", "08:00", "12:00", "16:00", "20:00" };
        public bool backgroundLoopStarted = false;
        AppConfig config;

        public int[] BrightnessLevels { get; private set; } = new int[6];

        public DateTimeController()
        {
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            this.Text = "Настройка яркости по времени";
            this.Size = new Size(247, 326);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;

            for (int i = 0; i < 6; i++)
            {
                var label = new Label
                {
                    Text = $"{i * 4:00}:00 - {(i + 1) * 4 % 24:00}:00",
                    Location = new Point(20, 20 + i * 35),
                    AutoSize = true
                };

                var numeric = new NumericUpDown
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = 50,
                    Width = 60,
                    Location = new Point(150, 18 + i * 35)
                };

                timeLabels[i] = label;
                brightnessInputs[i] = numeric;

                if (config == null)
                    config = AppConfig.Load();
                if (config.timedBrightness.TryGetValue(timeKeys[i], out int savedVal))
                {
                    brightnessInputs[i].Value = savedVal;
                }

                this.Controls.Add(label);
                this.Controls.Add(numeric);
            }

            var checkbox = new CheckBox
            {
                Checked = config.useTimedSettings,
                Text = "Использвать автосмену яркости",
                Location = new Point(20, 228),
                AutoSize = true
            };
            checkbox.CheckedChanged += (s, a) => { config.useTimedSettings = checkbox.Checked; };
            this.Controls.Add(checkbox);

            saveButton = new Button
            {
                Text = "Сохранить",
                Width = 221,
                Height = 30,
                Location = new Point(5, 252)
            };
            saveButton.Click += SaveButton_Click;

            this.Controls.Add(saveButton);
        }

        private async void SaveButton_Click(object sender, EventArgs e)
        {
            if (config == null)
                config = AppConfig.Load();

            for (int i = 0; i < 6; i++)
            {
                config.timedBrightness[timeKeys[i]] = (int)brightnessInputs[i].Value;
            }

            config.Save();
            await Form1.GetInstance.PrepareFormAsync();
            MessageBox.Show("Настройки яркости сохранены!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        public int GetBrightnessForCurrentTime()
        {
            int hour = DateTime.Now.Hour;
            int index = hour / 4;
            return (int)brightnessInputs[index].Value;
        }

        public bool CheckBrightnessTime()
        {
            string now = DateTime.Now.ToString("HH:mm");

            foreach (var key in timeKeys)
            {
                if (now == key)
                {
                    if (config == null)
                        config = AppConfig.Load();
                    if (config.useTimedSettings)
                    {
                        return true;
                    }
                    break;
                }
            }
            return false;
        }
    }
}

