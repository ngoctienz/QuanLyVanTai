using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using Core.Configs;

namespace QuanLyVanTai.UI.Components
{
    public enum SeatStatus
    {
        Empty,    // Ghế trống
        Booked,   // Đã đặt
        Selected  // Đang chọn
    }

    public class SeatItem
    {
        public string SeatNumber { get; set; } = string.Empty;
        public SeatStatus Status { get; set; } = SeatStatus.Empty;
        public int Floor { get; set; } = 1; // Tầng 1 hoặc Tầng 2
        public Rectangle Bounds { get; set; }
        public bool IsHovered { get; set; }
    }

    public class SeatMapControl : Control
    {
        private readonly List<SeatItem> _seats = new();
        private readonly ToolTip _toolTip = new();
        private SeatItem? _hoveredSeat = null;
        private int _totalSeats = 36;
        private HashSet<string> _bookedSeats = new(StringComparer.OrdinalIgnoreCase);
        private int _maxSelectableSeats = 5;

        [Category("SeatMap")]
        public event EventHandler? SelectionChanged;

        public SeatMapControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            this.DoubleBuffered = true;
            this.BackColor = Color.White;
            this.Font = ThemeConfig.MainFont;

            // Nạp dữ liệu ghế mặc định khi thiết kế
            GenerateSeats();
        }

        [Category("SeatMap")]
        [DefaultValue(36)]
        public int TotalSeats
        {
            get => _totalSeats;
            set
            {
                if (value <= 0) value = 36;
                _totalSeats = value;
                GenerateSeats();
                Invalidate();
            }
        }

        [Category("SeatMap")]
        [DefaultValue(5)]
        public int MaxSelectableSeats
        {
            get => _maxSelectableSeats;
            set => _maxSelectableSeats = Math.Max(1, value);
        }

        public void SetSeats(int totalSeats, IEnumerable<string>? bookedSeatNumbers)
        {
            _totalSeats = totalSeats > 0 ? totalSeats : 36;
            _bookedSeats = new HashSet<string>(bookedSeatNumbers ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            GenerateSeats();
            Invalidate();
        }

        public List<string> GetSelectedSeats()
        {
            return _seats
                .Where(s => s.Status == SeatStatus.Selected)
                .Select(s => s.SeatNumber)
                .ToList();
        }

        public void ClearSelection()
        {
            bool changed = false;
            foreach (var seat in _seats.Where(s => s.Status == SeatStatus.Selected))
            {
                seat.Status = SeatStatus.Empty;
                changed = true;
            }
            if (changed)
            {
                Invalidate();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void GenerateSeats()
        {
            _seats.Clear();
            _hoveredSeat = null;

            bool isDoubleDeck = _totalSeats > 20; // Xe trên 20 chỗ chia làm 2 tầng (Ví dụ: Giường nằm)
            int seatsPerFloor = isDoubleDeck ? (int)Math.Ceiling(_totalSeats / 2.0) : _totalSeats;

            // Sinh ghế tầng 1
            for (int i = 1; i <= seatsPerFloor; i++)
            {
                string code = isDoubleDeck ? $"A{i:D2}" : $"{i:D2}";
                var status = _bookedSeats.Contains(code) ? SeatStatus.Booked : SeatStatus.Empty;
                _seats.Add(new SeatItem { SeatNumber = code, Status = status, Floor = 1 });
            }

            // Sinh ghế tầng 2 (nếu có)
            if (isDoubleDeck)
            {
                int remaining = _totalSeats - seatsPerFloor;
                for (int i = 1; i <= remaining; i++)
                {
                    string code = $"B{i:D2}";
                    var status = _bookedSeats.Contains(code) ? SeatStatus.Booked : SeatStatus.Empty;
                    _seats.Add(new SeatItem { SeatNumber = code, Status = status, Floor = 2 });
                }
            }

            RecalculateLayout();
        }

        private void RecalculateLayout()
        {
            if (Width <= 0 || Height <= 0) return;

            bool isDoubleDeck = _totalSeats > 20;
            int legendHeight = 45;
            int availableHeight = Height - legendHeight - 20;
            int availableWidth = Width - 20;

            int floorWidth = isDoubleDeck ? (availableWidth - 30) / 2 : availableWidth;
            int startY = legendHeight + 10;

            // Layout từng tầng
            LayoutFloor(1, 10, startY, floorWidth, availableHeight);

            if (isDoubleDeck)
            {
                LayoutFloor(2, 10 + floorWidth + 20, startY, floorWidth, availableHeight);
            }
        }

        private void LayoutFloor(int floorNum, int startX, int startY, int floorW, int floorH)
        {
            var floorSeats = _seats.Where(s => s.Floor == floorNum).ToList();
            if (floorSeats.Count == 0) return;

            int cols = 3; // 3 dãy ghế (Ví dụ: A - Lối đi - B - C)
            int rows = (int)Math.Ceiling(floorSeats.Count / (double)cols);

            int seatW = Math.Min(65, Math.Max(42, (floorW - 60) / cols));
            int seatH = Math.Min(55, Math.Max(38, (floorH - 50) / (rows + 1)));

            int marginX = (floorW - (cols * seatW + 25)) / 2;
            if (marginX < 10) marginX = 10;

            int topHeader = 35; // Khoảng trống cho vô lăng / tiêu đề tầng

            for (int i = 0; i < floorSeats.Count; i++)
            {
                int r = i / cols;
                int c = i % cols;

                // Tọa độ X với lối đi giữa cột 1 và 2
                int xGap = c >= 1 ? 20 : 0;
                int x = startX + marginX + c * seatW + xGap;
                int y = startY + topHeader + r * (seatH + 10);

                floorSeats[i].Bounds = new Rectangle(x, y, seatW, seatH);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RecalculateLayout();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Background
            using (var bgBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            // 1. Vẽ Chú Thích Màu Sắc (Legend)
            DrawLegend(g);

            bool isDoubleDeck = _totalSeats > 20;

            if (isDoubleDeck)
            {
                int legendHeight = 45;
                int floorWidth = (Width - 50) / 2;
                int floorHeight = Height - legendHeight - 25;

                DrawFloorBox(g, "TẦNG DƯỚI (TẦNG 1)", 10, legendHeight + 5, floorWidth, floorHeight);
                DrawFloorBox(g, "TẦNG TRÊN (TẦNG 2)", 10 + floorWidth + 20, legendHeight + 5, floorWidth, floorHeight);
            }
            else
            {
                int legendHeight = 45;
                DrawFloorBox(g, "SƠ ĐỒ GHẾ XE KHÁCH", 10, legendHeight + 5, Width - 20, Height - legendHeight - 25);
            }

            // 2. Vẽ từng Ghế
            foreach (var seat in _seats)
            {
                DrawSeat(g, seat);
            }
        }

        private void DrawLegend(Graphics g)
        {
            int y = 12;
            int itemW = 110;
            int startX = (Width - (itemW * 3)) / 2;
            if (startX < 10) startX = 10;

            // 1. Trống
            DrawLegendItem(g, startX, y, Color.White, ThemeConfig.PrimaryColor, "Ghế trống");

            // 2. Đang chọn
            DrawLegendItem(g, startX + itemW, y, ThemeConfig.SuccessColor, ThemeConfig.SuccessColor, "Đang chọn", Color.White);

            // 3. Đã đặt
            DrawLegendItem(g, startX + itemW * 2, y, Color.FromArgb(226, 232, 240), Color.FromArgb(148, 163, 184), "Đã đặt", Color.FromArgb(100, 116, 139));
        }

        private void DrawLegendItem(Graphics g, int x, int y, Color fillColor, Color borderColor, string text, Color? textColor = null)
        {
            var rect = new Rectangle(x, y, 20, 18);
            using (var path = GetRoundedRectanglePath(rect, 4))
            {
                using var fill = new SolidBrush(fillColor);
                using var pen = new Pen(borderColor, 1.5f);
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }

            using var font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            using var textBrush = new SolidBrush(textColor ?? ThemeConfig.TextMain);
            g.DrawString(text, font, textBrush, x + 24, y + 1);
        }

        private void DrawFloorBox(Graphics g, string title, int x, int y, int w, int h)
        {
            var rect = new Rectangle(x, y, w, h);
            using (var path = GetRoundedRectanglePath(rect, 8))
            {
                using var fill = new SolidBrush(Color.White);
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1.5f);
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }

            // Tiêu đề Tầng / Bán kính
            using (var headerFont = new Font("Segoe UI", 9F, FontStyle.Bold))
            using (var headerBrush = new SolidBrush(ThemeConfig.PrimaryColor))
            {
                g.DrawString(title, headerFont, headerBrush, x + 12, y + 10);
            }

            // Vẽ biểu tượng Vô lăng tài xế ở góc trên phải của khung
            using (var driverFont = new Font("Segoe UI", 8.5F, FontStyle.Italic))
            using (var driverBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                g.DrawString("🚘 Lái xe", driverFont, driverBrush, x + w - 65, y + 10);
            }
        }

        private void DrawSeat(Graphics g, SeatItem seat)
        {
            if (seat.Bounds.Width <= 0 || seat.Bounds.Height <= 0) return;

            Color bgColor;
            Color borderColor;
            Color textColor;

            switch (seat.Status)
            {
                case SeatStatus.Selected:
                    bgColor = ThemeConfig.SuccessColor;
                    borderColor = Color.FromArgb(5, 150, 105);
                    textColor = Color.White;
                    break;

                case SeatStatus.Booked:
                    bgColor = Color.FromArgb(226, 232, 240);
                    borderColor = Color.FromArgb(203, 213, 225);
                    textColor = Color.FromArgb(148, 163, 184);
                    break;

                case SeatStatus.Empty:
                default:
                    if (seat.IsHovered)
                    {
                        bgColor = Color.FromArgb(239, 246, 255); // Blue-50
                        borderColor = ThemeConfig.PrimaryColor;
                        textColor = ThemeConfig.PrimaryColor;
                    }
                    else
                    {
                        bgColor = Color.White;
                        borderColor = Color.FromArgb(203, 213, 225);
                        textColor = ThemeConfig.TextMain;
                    }
                    break;
            }

            // Vẽ khối ghế chính (thân ghế)
            var rect = seat.Bounds;
            using (var path = GetRoundedRectanglePath(rect, 6))
            {
                using var fill = new SolidBrush(bgColor);
                using var pen = new Pen(borderColor, seat.IsHovered || seat.Status == SeatStatus.Selected ? 2f : 1.2f);
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }

            // Vẽ gối tựa đầu phía trên của ghế
            var headRestRect = new Rectangle(rect.X + 6, rect.Y + 3, rect.Width - 12, 6);
            using (var headPath = GetRoundedRectanglePath(headRestRect, 3))
            {
                using var headFill = new SolidBrush(seat.Status == SeatStatus.Selected ? Color.FromArgb(5, 150, 105) : borderColor);
                g.FillPath(headFill, headPath);
            }

            // Vẽ số ghế
            using var font = new Font("Segoe UI", 9F, FontStyle.Bold);
            using var textBrush = new SolidBrush(textColor);
            var stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            var textRect = new Rectangle(rect.X, rect.Y + 6, rect.Width, rect.Height - 6);
            g.DrawString(seat.SeatNumber, font, textBrush, textRect, stringFormat);
        }

        // ==========================================
        // EVENT HANDLERS FOR MOUSE INTERACTION
        // ==========================================
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            SeatItem? found = _seats.FirstOrDefault(s => s.Bounds.Contains(e.Location));

            if (found != _hoveredSeat)
            {
                if (_hoveredSeat != null)
                {
                    _hoveredSeat.IsHovered = false;
                }

                _hoveredSeat = found;

                if (_hoveredSeat != null)
                {
                    _hoveredSeat.IsHovered = true;

                    string tip = _hoveredSeat.Status switch
                    {
                        SeatStatus.Booked => $"Ghế {_hoveredSeat.SeatNumber}: ĐÃ ĐẶT",
                        SeatStatus.Selected => $"Ghế {_hoveredSeat.SeatNumber}: ĐANG CHỌN (Nhấp để bỏ chọn)",
                        _ => $"Ghế {_hoveredSeat.SeatNumber}: GHẾ TRỐNG (Nhấp để chọn)"
                    };
                    _toolTip.SetToolTip(this, tip);
                    this.Cursor = _hoveredSeat.Status == SeatStatus.Booked ? Cursors.Default : Cursors.Hand;
                }
                else
                {
                    _toolTip.SetToolTip(this, null);
                    this.Cursor = Cursors.Default;
                }

                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoveredSeat != null)
            {
                _hoveredSeat.IsHovered = false;
                _hoveredSeat = null;
                this.Cursor = Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (e.Button != MouseButtons.Left) return;

            SeatItem? clickedSeat = _seats.FirstOrDefault(s => s.Bounds.Contains(e.Location));
            if (clickedSeat == null || clickedSeat.Status == SeatStatus.Booked)
                return;

            if (clickedSeat.Status == SeatStatus.Selected)
            {
                clickedSeat.Status = SeatStatus.Empty;
                Invalidate();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
            else if (clickedSeat.Status == SeatStatus.Empty)
            {
                int currentSelected = _seats.Count(s => s.Status == SeatStatus.Selected);
                if (currentSelected >= _maxSelectableSeats)
                {
                    MessageBox.Show(
                        $"Bạn chỉ được chọn tối đa {_maxSelectableSeats} ghế cho mỗi lần đặt vé!",
                        "Thông Báo Giới Hạn",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                clickedSeat.Status = SeatStatus.Selected;
                Invalidate();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private static GraphicsPath GetRoundedRectanglePath(Rectangle rect, int cornerRadius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = cornerRadius * 2;

            if (rect.Width < diameter || rect.Height < diameter)
            {
                path.AddRectangle(rect);
                return path;
            }

            Rectangle arc = new Rectangle(rect.X, rect.Y, diameter, diameter);

            // Top Left
            path.AddArc(arc, 180, 90);

            // Top Right
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Bottom Right
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Bottom Left
            arc.X = rect.X;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _toolTip.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
