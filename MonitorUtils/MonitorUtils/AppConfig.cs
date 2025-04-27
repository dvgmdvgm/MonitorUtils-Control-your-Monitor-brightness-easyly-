using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace MonitorUtils
{
    public class AppConfig
    {
        public bool linkSliders { get; set; }
        public int autostartBrightness { get; set; }
        public Dictionary<int, int> monitorBrightness { get; set; } = new Dictionary<int, int>();
        public bool useTimedSettings { get; set; } = false;
        public Dictionary<string, int> timedBrightness { get; set; } = new Dictionary<string, int>
        {
            { "00:00", 50 },
            { "04:00", 50 },
            { "08:00", 50 },
            { "12:00", 50 },
            { "16:00", 50 },
            { "20:00", 50 }
        };
        private static readonly string configPath = Form1.GetInstance.appPath + @"settings.json";

        public static AppConfig Load()
        {
            if (!File.Exists(configPath))
            {
                // Если файла нет — создаём дефолтный
                var defaultConfig = new AppConfig
                {
                    linkSliders = false,
                    autostartBrightness = 75,
                    monitorBrightness = new Dictionary<int, int>(),
                    useTimedSettings = false 
                    //MonitorName = "Default Monitor"
                };
                defaultConfig.Save();
                return defaultConfig;
            }

            string json; using (StreamReader sr = new StreamReader(configPath)) { json = sr.ReadToEnd(); };
            return JsonConvert.DeserializeObject<AppConfig>(json);
        }

        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            using (StreamWriter sw = new StreamWriter(configPath))
            {
                sw.Write(json);
            }
        }

        public void SaveMonitorBrightness(int monitorIndex, int brightness)
        {
            monitorBrightness[monitorIndex] = brightness;
            Save();
        }

        public int GetMonitorBrightness(int monitorIndex)
        {
            if (monitorBrightness.TryGetValue(monitorIndex, out int value))
                return value;
            return autostartBrightness;
        }
    }
}
