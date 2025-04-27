using Microsoft.Win32;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MonitorUtils
{
    static class Program
    {
        static EventHandler displayChangedHandler;
        [STAThread]
        static void Main()
        {

            if (!InstanceChecker.TakeMemory())
            {
                Application.Exit();
                return;
            }
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                var form = Form1.GetInstance;
                var handle = form.Handle;
                form.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                form.PrepareFormAsync().GetAwaiter();
                //form.Hide(); 
                var tray = TrayManager.GetInstance;
                displayChangedHandler = async (s, e) =>
                {
                    //form.shouldReload = true;
                    await Task.Run(async () =>
                    {
                        await Task.Delay(7000); 

                        form.BeginInvoke((Action)(() =>
                            {
                                form.PrepareFormAsync().ContinueWith(t =>
                                {
                                    if (t.Exception != null)
                                    {
                                        MessageBox.Show(t.Exception.ToString(), " Ошибка при настройке при смене разрешения");
                                    }
                                });
                            }));
                    });
                };
                SystemEvents.DisplaySettingsChanged += displayChangedHandler;
                Application.Run();
                tray.Dispose();
            }
            finally
            {
                //AppConfig.GetInstance.Save();
                SystemEvents.DisplaySettingsChanged -= displayChangedHandler;
                InstanceChecker.ReleaseMemory();
            }
        }
    }
}
