using MonitorUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public class MonitorManager
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
        MonitorEnumProc lpfnEnum, IntPtr dwData);

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor,
        ref Rect lprcMonitor, IntPtr dwData);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetMonitorBrightness(IntPtr hMonitor, out uint minimumBrightness,
        out uint currentBrightness, out uint maximumBrightness);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool SetMonitorBrightness(IntPtr hMonitor, uint newBrightness);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint count);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint count,
        [Out] PhysicalMonitor[] monitors);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool DestroyPhysicalMonitors(uint count, PhysicalMonitor[] monitors);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct PhysicalMonitor
    {
        public IntPtr hPhysicalMonitor;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szPhysicalMonitorDescription;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    public class MonitorInfo
    {
        public string Name; // Описание монитора
        public string Model; // Понятное имя монитора
        public IntPtr Handle; // Дескриптор физического монитора
        public uint Min; // Минимальная яркость
        public uint Max; // Максимальная яркость
        public uint Current; // Текущая яркость
    }

    // Метод для получения списка мониторов и их яркости
    public static List<MonitorInfo> GetMonitors()
    {
        List<MonitorInfo> monitors = new List<MonitorInfo>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (MonitorEnumProc)((IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData) =>
        {
            if (GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out uint count))
            {
                var physicalMonitors = new PhysicalMonitor[count];
                if (GetPhysicalMonitorsFromHMONITOR(hMonitor, count, physicalMonitors))
                {
                    for (int i = 0; i < count; i++)
                    {
                        var m = physicalMonitors[i];
                        if (GetMonitorBrightness(m.hPhysicalMonitor, out uint min, out uint current, out uint max))
                        {
                            // Получаем имя монитора
                            monitors.Add(new MonitorInfo
                            {
                                Name = m.szPhysicalMonitorDescription,
                                Handle = m.hPhysicalMonitor,
                                Min = min,
                                Max = max,
                                Current = current
                            });
                        }
                    }
                }
            }
            return true;
        }), IntPtr.Zero);

        return monitors;
    }

    // Устанавливаем яркость монитора
    public static void SetBrightness(IntPtr handle, uint brightness)
    {
        SetMonitorBrightness(handle, brightness);
    }

    public static class MonitorPowerManager
    {
        const int SC_MONITORPOWER = 0xF170;
        const int WM_SYSCOMMAND = 0x0112;
        static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BlockInput(bool fBlockIt);


        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);


        private static List<MonitorControl> _controls = new();

        public static void RegisterControls(IEnumerable<MonitorControl> controls)
        {
            _controls = controls.ToList();
        }

        public static Task TurnOffMonitorsAsync()
        {
            return Task.Run(async () =>
            {
                BlockInput(true);
                Form1.GetInstance.Hide();
                await Task.Delay(100);
                SendMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, 2);
                await Task.Delay(3000);
                BlockInput(false);
            });
        }


        public static void TurnOnMonitors()
        {
            mouse_event(0, 0, 0, 0, 0);
        }

        public static async Task RestoreAllBrightness()
        {
            await Task.Delay(5000); // Подождём, пока мониторы точно включатся

            foreach (var ctrl in _controls)
            {
                ctrl.SetBrightnessSilent(ctrl.Slider.Value);
            }
        }
    }

}
