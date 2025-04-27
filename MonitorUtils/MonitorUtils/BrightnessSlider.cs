using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static MonitorManager;

namespace MonitorUtils
{
    public class BrightnessSlider : Control
    {
        public static BrightnessSlider bs;
        private int _value;
        private int _min;
        private int _max;
        private bool _hover;
        private bool _dragging;
        private RectangleF _thumbRect;
        private Color _primaryColor;
        private Color _trackColor;
        private Color _thumbHoverColor;
        private Color _textColor;
        private readonly bool _isDarkTheme;
        AppConfig config;
        DateTimeController dt;

        public event Action<int> ValueChanged;

        public Color PrimaryColor
        {
            get => _primaryColor;
            set { _primaryColor = value; Redraw(); }
        }

        public Color TrackColor
        {
            get => _trackColor;
            set { _trackColor = value; Redraw(); }
        }

        public Color ThumbHoverColor
        {
            get => _thumbHoverColor;
            set { _thumbHoverColor = value; Redraw(); }
        }

        public Color TextColor
        {
            get => _textColor;
            set { _textColor = value; Redraw(); }
        }

        public int Value
        {
            get => _value;
            set
            {
                value = Math.Max(_min, Math.Min(_max, value));
                if (_value != value)
                {
                    _value = value;
                    ValueChanged?.Invoke(_value);
                    Redraw();
                }
            }
        }

        public int Minimum
        {
            get => _min;
            set { _min = value; Redraw(); }
        }

        public int Maximum
        {
            get => _max;
            set { _max = value; Redraw(); }
        }

        public BrightnessSlider(bool isDarkTheme)
        {
            _isDarkTheme = isDarkTheme;
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.Opaque, true);
            DoubleBuffered = true;
            Height = 50;
        }

        public void SetValueSilent(int value)
        {
            value = Math.Max(_min, Math.Min(_max, value));
            if (_value != value)
            {
                _value = value;
                Redraw();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_dragging)
            {
                Value = CalculateValueFromPosition(e.X);
                Redraw(); // <–– принудительно отрисовываем каждое движение
            }
            else
            {
                bool newHover = _thumbRect.Contains(e.Location);
                if (newHover != _hover)
                {
                    _hover = newHover;
                    Redraw();
                }
            }
        }


        private int CalculateValueFromPosition(int x)
        {
            float percent = (x - 15) / (float)(Width - 30);
            percent = Math.Max(0, Math.Min(1, percent));
            return (int)(_min + percent * (_max - _min));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _dragging = true;
                Value = CalculateValueFromPosition(e.X);
                Capture = true;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (config == null)
                config = AppConfig.Load();
            //Form1.GetInstance.shouldReload = true;
            base.OnMouseUp(e);
            if (_dragging)
            {
                _dragging = false;
                Capture = false;
                Redraw();
            }
            for (int i = 0; i < Form1.GetInstance.monitors.Count; i++)
            {
                config.SaveMonitorBrightness(i, Form1.GetInstance.monitorControls[i].Slider.Value);
            }
            //config.useTimedSettings = false;
            config.Save();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            Redraw();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;

            // Очистка фона
            g.Clear(BackColor);

            // Рисуем трек
            var trackRect = new RectangleF(15, Height / 2 - 4, Width - 30, 8);
            using (var brush = new SolidBrush(TrackColor))
                g.FillRectangle(brush, trackRect);

            // Заполненная часть
            float fillWidth = Math.Max(1, (_value - _min) / (float)(_max - _min) * (Width - 30));
            var fillRect = new RectangleF(15, Height / 2 - 4, fillWidth, 8);
            using (var brush = new LinearGradientBrush(fillRect, PrimaryColor,
                Color.FromArgb(PrimaryColor.R / 2, PrimaryColor.G / 2, PrimaryColor.B / 2),
                LinearGradientMode.Horizontal))
                g.FillRectangle(brush, fillRect);

            // Ползунок
            float thumbPos = 15 + (_value - _min) / (float)(_max - _min) * (Width - 30);
            _thumbRect = new RectangleF(thumbPos - 10, Height / 2 - 10, 20, 20);

            var thumbColor = _dragging ? PrimaryColor : _hover ? ThumbHoverColor : PrimaryColor;
            using (var brush = new SolidBrush(thumbColor))
            using (var pen = new Pen(_isDarkTheme ? Color.FromArgb(50, 50, 50) : Color.White, 2))
            {
                g.FillEllipse(brush, _thumbRect);
                g.DrawEllipse(pen, _thumbRect);
            }

            // Подписи
            using (var font = new Font("Segoe UI", 8))
            using (var brush = new SolidBrush(TextColor))
            {
                g.DrawString(_min.ToString(), font, brush, 15, Height / 2 + 15);
                g.DrawString(_max.ToString(), font, brush, Width - 30, Height / 2 + 15);
            }
        }

        public void Redraw()
        {
            if (IsHandleCreated && Visible)
            {
                Invalidate();
            }
        }
    }

}
