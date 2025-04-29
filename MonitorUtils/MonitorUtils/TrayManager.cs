using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MonitorUtils
{
    public class TrayManager : NativeWindow, IDisposable
    {
        #region Static
        // Получить Instance класса
        static volatile TrayManager thisClass;
        static object SyncObject = new object();
        public static TrayManager GetInstance
        {
            get
            {
                if (thisClass == null)
                    lock (SyncObject)
                    {
                        if (thisClass == null)
                            thisClass = new TrayManager();
                    }
                return thisClass;
            }
        }
        #endregion

        private const int WM_USER = 0x0400;
        private const int WM_TRAYMESSAGE = WM_USER + 1;
        private const int NIF_MESSAGE = 0x00000001;
        private const int NIF_ICON = 0x00000002;
        private const int NIF_TIP = 0x00000004;
        private const int NIM_ADD = 0x00000000;
        private const int NIM_MODIFY = 0x00000001;
        private const int NIM_DELETE = 0x00000002;

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int Shell_NotifyIcon(uint dwMessage, [In] ref NOTIFYICONDATA lpdata);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int Shell_NotifyIconGetRect(ref NOTIFYICONIDENTIFIER identifier, out RECT iconRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct NOTIFYICONDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NOTIFYICONIDENTIFIER
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public Guid guidItem;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left, top, right, bottom;
        }

        private const uint TrayIconId = 1;

        private readonly Form1 mainForm;
        private readonly IntPtr hIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath).Handle;

        public TrayManager()
        {
            mainForm = Form1.GetInstance;

            CreateHandle(new CreateParams());

            var data = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = Handle,
                uID = TrayIconId,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_TRAYMESSAGE,
                hIcon = hIcon,
                szTip = "Яркость монитора: " + MonitorManager.GetMonitors()[0].Current
            };

            Shell_NotifyIcon(NIM_ADD, ref data);
            //ShowFormAboveTray(); // Показать форму при запуске программы....
        }

        public void UpdateTooltip()
        {
            var monitors = MonitorManager.GetMonitors();
            string tipText = monitors.Count > 0
                ? "Яркость монитора " + monitors[0].Current
                : "Монитор не найден";

            var data = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = Handle,
                uID = TrayIconId,
                uFlags = NIF_TIP,
                szTip = tipText
            };

            Shell_NotifyIcon(NIM_MODIFY, ref data);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_TRAYMESSAGE)
            {
                int mouseMsg = m.LParam.ToInt32();
                if (mouseMsg == 0x0202) // WM_LBUTTONUP
                {
                    _=ShowFormAboveTray();
                }
                else if (mouseMsg == 0x0204) // WM_RBUTTONUP
                {
                    ShowContextMenu();
                }
            }

            base.WndProc(ref m);
        }

        int iconCenterX;
        int x;
        int y;
        Screen screen;

        public async Task ShowFormAboveTray()
        {
            //mainForm.Opacity = 0;
            var id = new NOTIFYICONIDENTIFIER
            {
                cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONIDENTIFIER)),
                hWnd = Handle,
                uID = TrayIconId
            };
            mainForm.Hide();

            if (!mainForm.Visible)
            {
                if (Shell_NotifyIconGetRect(ref id, out RECT rect) == 0)
                {
                    {
                        iconCenterX = (rect.left + rect.right) / 2;
                        x = iconCenterX - mainForm.Width / 2;
                        y = rect.top - mainForm.Height;

                        screen = Screen.FromPoint(new Point(iconCenterX, rect.top));
                        x = Math.Max(screen.WorkingArea.Left, Math.Min(x, screen.WorkingArea.Right - mainForm.Width));
                        y = Math.Max(screen.WorkingArea.Top, y);

                    }
                    iconCenterX = (rect.left + rect.right) / 2;
                    x = iconCenterX - mainForm.Width / 2;
                    y = rect.top - mainForm.Height;

                    screen = Screen.FromPoint(new Point(iconCenterX, rect.top));
                    x = Math.Max(screen.WorkingArea.Left, Math.Min(x, screen.WorkingArea.Right - mainForm.Width));
                    y = Math.Max(screen.WorkingArea.Top, y);

                    //if (mainForm.shouldReload)
                    //{
                    //    await mainForm.PrepareFormAsync();
                    //    mainForm.shouldReload = false;
                    //}

                    // Начальная позиция ниже финальной
                    int finalY = screen.WorkingArea.Bottom - mainForm.Height - 4;
                    int startY = finalY + 20;
                    mainForm.Location = new Point(x - 138, startY);

                    // Начальная прозрачность
                    mainForm.Opacity = 0;
                    mainForm.Show();
                    mainForm.Activate();


                    Timer animationTimer = new Timer();
                    animationTimer.Interval = 2;

                    animationTimer.Tick += (s, e) =>
                    {
                        if (mainForm.Opacity < 1.0)
                            mainForm.Opacity += 0.05;

                        if (mainForm.Location.Y > finalY)
                            mainForm.Location = new Point(mainForm.Location.X, mainForm.Location.Y - 2);

                        if (mainForm.Opacity >= 1.0 && mainForm.Location.Y <= finalY)
                        {
                            mainForm.Opacity = 1;
                            mainForm.Location = new Point(mainForm.Location.X, finalY);
                            animationTimer.Stop();
                            animationTimer.Dispose();
                        }
                    };

                    animationTimer.Start();
                }
            }
        }


        private void ShowContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Выход", null, (s, e) => Application.Exit());

            Point cursor = Cursor.Position;
            menu.Show(cursor);
        }

        public void Dispose()
        {
            var data = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = Handle,
                uID = TrayIconId
            };

            Shell_NotifyIcon(NIM_DELETE, ref data);
            DestroyHandle();
        }
    }
}
