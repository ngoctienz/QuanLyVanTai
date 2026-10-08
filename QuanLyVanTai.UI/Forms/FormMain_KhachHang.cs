using Core.Configs;
using Core.Helpers;
using FontAwesome.Sharp;
using QuanLyVanTai.BLL.Services;
using QuanLyVanTai.DAL.Models;
using QuanLyVanTai.UI.Components;
using QuanLyVanTai.UI.Helpers;

namespace QuanLyVanTai.UI.Forms
{
    public partial class FormMain_KhachHang : Form
    {
        // ==========================================
        // SERVICES
        // ==========================================
        private readonly RouteService _routeService = new();
        private readonly TicketService _ticketService = new();

        // ==========================================
        // UI CONTROLS - HEADER
        // ==========================================
        private readonly Panel pnlHeader = new();
        private readonly Label lblBrand = new();
        private readonly Label lblUserInfo = new();
        private readonly IconButton btnLogout = new();

        // ==========================================
        // UI CONTROLS - LEFT PANEL (CHOOSING ROUTE & SEAT MAP)
        // ==========================================
        private readonly Panel pnlLeft = new();
        private readonly GroupBox grpTripSelect = new();
        private readonly ComboBox cboRoutes = new();
        private readonly ComboBox cboVehicles = new();
        private readonly DateTimePicker dtpDepartureDate = new();
        private readonly SeatMapControl seatMapControl = new();

        // ==========================================
        // UI CONTROLS - RIGHT PANEL (CUSTOMER INFO & BOOKING)
        // ==========================================
        private readonly Panel pnlRight = new();
        private readonly GroupBox grpCustomerInfo = new();
        private readonly TextBox txtCustomerName = new();
        private readonly TextBox txtCustomerPhone = new();
        private readonly TextBox txtCustomerEmail = new();

        private readonly GroupBox grpSummary = new();
        private readonly Label lblSelectedSeatsCount = new();
        private readonly Label lblSelectedSeatNumbers = new();
        private readonly Label lblBaseUnitPrice = new();
        private readonly ComboBox cboPromotions = new();
        private readonly Label lblDiscountAmount = new();
        private readonly Label lblTotalPrice = new();

        private readonly IconButton btnConfirmBooking = new();
        private readonly ErrorProvider epValidation = new();

        // ==========================================
        // STATE
        // ==========================================
        private List<Route> _routes = new();
        private List<Vehicle> _vehicles = new();
        private Route? _selectedRoute = null;
        private Vehicle? _selectedVehicle = null;
        private decimal _currentDiscountAmount = 0m;
        private decimal _finalTotalPrice = 0m;
        private List<Ticket> _recentlyBookedTickets = new();

        public FormMain_KhachHang()
        {
            this.Font = ThemeConfig.MainFont;
            this.Text = "Hệ Thống Đặt Vé Xe Khách Trực Tuyến - Dành Cho Khách Hàng";
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ThemeConfig.BackgroundColor;

            BuildUILayout();
            WireEvents();
            LoadUserData();

            this.Load += async (s, e) => await LoadInitialTripDataAsync();
        }

        // ==========================================
        // BUILD UI LAYOUT
        // ==========================================
        private void BuildUILayout()
        {
            // --- 1. HEADER BAR ---
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 65;
            pnlHeader.BackColor = ThemeConfig.PrimaryColor;
            pnlHeader.Padding = new Padding(20, 10, 20, 10);

            lblBrand.Text = "🚌 VẬN TẢI HÀNH KHÁCH - ĐẶT VÉ TRỰC TUYẾN";
            lblBrand.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblBrand.ForeColor = Color.White;
            lblBrand.AutoSize = true;
            lblBrand.Location = new Point(20, 18);

            lblUserInfo.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            lblUserInfo.ForeColor = Color.White;
            lblUserInfo.AutoSize = true;
            lblUserInfo.TextAlign = ContentAlignment.MiddleRight;

            ThemeConfig.StyleDangerButton(btnLogout, IconChar.RightFromBracket);
            btnLogout.Text = " Đăng xuất";
            btnLogout.Size = new Size(120, 38);
            btnLogout.Click += BtnLogout_Click;

            pnlHeader.Controls.Add(lblBrand);
            pnlHeader.Controls.Add(lblUserInfo);
            pnlHeader.Controls.Add(btnLogout);

            pnlHeader.Resize += (s, e) => RepositionHeaderControls();

            this.Controls.Add(pnlHeader);

            // --- MAIN CONTAINER ---
            var pnlContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(15)
            };
            pnlContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            pnlContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            // --- 2. LEFT PANEL (Chuyến xe & Sơ đồ ghế) ---
            pnlLeft.Dock = DockStyle.Fill;
            pnlLeft.BackColor = Color.White;
            pnlLeft.Padding = new Padding(15);

            // GroupBox Chọn Chuyến
            grpTripSelect.Text = "1. CHỌN TUYẾN XE & CHUYẾN ĐI";
            grpTripSelect.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            grpTripSelect.ForeColor = ThemeConfig.PrimaryColor;
            grpTripSelect.Dock = DockStyle.Top;
            grpTripSelect.Height = 135;

            var lblR = new Label { Text = "Tuyến xe:", Location = new Point(15, 30), AutoSize = true, Font = ThemeConfig.MainFont, ForeColor = ThemeConfig.TextMain };
            cboRoutes.Location = new Point(15, 52);
            cboRoutes.Size = new Size(320, 28);
            cboRoutes.DropDownStyle = ComboBoxStyle.DropDownList;
            cboRoutes.Font = ThemeConfig.MainFont;

            var lblD = new Label { Text = "Ngày đi:", Location = new Point(350, 30), AutoSize = true, Font = ThemeConfig.MainFont, ForeColor = ThemeConfig.TextMain };
            dtpDepartureDate.Location = new Point(350, 52);
            dtpDepartureDate.Size = new Size(160, 28);
            dtpDepartureDate.Format = DateTimePickerFormat.Short;
            dtpDepartureDate.MinDate = DateTime.Today;
            dtpDepartureDate.Font = ThemeConfig.MainFont;

            var lblV = new Label { Text = "Phương tiện / Chuyến:", Location = new Point(15, 88), AutoSize = true, Font = ThemeConfig.MainFont, ForeColor = ThemeConfig.TextMain };
            cboVehicles.Location = new Point(160, 85);
            cboVehicles.Size = new Size(350, 28);
            cboVehicles.DropDownStyle = ComboBoxStyle.DropDownList;
            cboVehicles.Font = ThemeConfig.MainFont;

            grpTripSelect.Controls.AddRange(new Control[] { lblR, cboRoutes, lblD, dtpDepartureDate, lblV, cboVehicles });
            pnlLeft.Controls.Add(grpTripSelect);

            // Sơ đồ ghế GDI+
            seatMapControl.Dock = DockStyle.Fill;
            seatMapControl.Margin = new Padding(0, 10, 0, 0);
            pnlLeft.Controls.Add(seatMapControl);
            grpTripSelect.SendToBack();

            // --- 3. RIGHT PANEL (Thông tin Khách & Thanh Toán) ---
            pnlRight.Dock = DockStyle.Fill;
            pnlRight.BackColor = Color.White;
            pnlRight.Padding = new Padding(15);

            // GroupBox Thông tin khách hàng
            grpCustomerInfo.Text = "2. THÔNG TIN HÀNH KHÁCH";
            grpCustomerInfo.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            grpCustomerInfo.ForeColor = ThemeConfig.PrimaryColor;
            grpCustomerInfo.Dock = DockStyle.Top;
            grpCustomerInfo.Height = 170;

            int y = 28;
            AddFormRow(grpCustomerInfo, "Họ và tên *:", txtCustomerName, y); y += 42;
            AddFormRow(grpCustomerInfo, "Số điện thoại *:", txtCustomerPhone, y); y += 42;
            AddFormRow(grpCustomerInfo, "Email liên hệ:", txtCustomerEmail, y);

            pnlRight.Controls.Add(grpCustomerInfo);

            // GroupBox Tổng hợp & Đặt vé
            grpSummary.Text = "3. CHI TIẾT ĐẶT VÉ & THANH TOÁN";
            grpSummary.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            grpSummary.ForeColor = ThemeConfig.PrimaryColor;
            grpSummary.Dock = DockStyle.Top;
            grpSummary.Height = 280;

            y = 30;
            AddSummaryRow(grpSummary, "Số ghế chọn:", lblSelectedSeatsCount, y, "0 ghế"); y += 35;
            AddSummaryRow(grpSummary, "Danh sách ghế:", lblSelectedSeatNumbers, y, "Chưa chọn"); y += 35;
            AddSummaryRow(grpSummary, "Đơn giá vé:", lblBaseUnitPrice, y, "0 VNĐ"); y += 35;

            var lblP = new Label { Text = "Mã khuyến mãi:", Location = new Point(15, y + 3), AutoSize = true, Font = ThemeConfig.MainFont, ForeColor = ThemeConfig.TextMain };
            cboPromotions.Location = new Point(160, y);
            cboPromotions.Size = new Size(200, 28);
            cboPromotions.DropDownStyle = ComboBoxStyle.DropDownList;
            cboPromotions.Font = ThemeConfig.MainFont;
            grpSummary.Controls.AddRange(new Control[] { lblP, cboPromotions });
            y += 38;

            AddSummaryRow(grpSummary, "Giảm giá:", lblDiscountAmount, y, "-0 VNĐ", ThemeConfig.SuccessColor); y += 35;
            AddSummaryRow(grpSummary, "TỔNG TIỀN:", lblTotalPrice, y, "0 VNĐ", ThemeConfig.DangerColor, new Font("Segoe UI", 13F, FontStyle.Bold));

            pnlRight.Controls.Add(grpSummary);

            // Button Confirm Booking
            ThemeConfig.StyleSuccessButton(btnConfirmBooking, IconChar.Ticket);
            btnConfirmBooking.Text = " XÁC NHẬN ĐẶT VÉ & XUẤT VÉ (PDF)";
            btnConfirmBooking.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
            btnConfirmBooking.Dock = DockStyle.Bottom;
            btnConfirmBooking.Height = 52;
            btnConfirmBooking.Cursor = Cursors.Hand;

            pnlRight.Controls.Add(btnConfirmBooking);
            grpSummary.SendToBack();

            pnlContainer.Controls.Add(pnlLeft, 0, 0);
            pnlContainer.Controls.Add(pnlRight, 1, 0);

            this.Controls.Add(pnlContainer);
            pnlHeader.SendToBack();
        }

        private void RepositionHeaderControls()
        {
            int vCenter = (pnlHeader.Height - 38) / 2;
            btnLogout.Location = new Point(pnlHeader.Width - btnLogout.Width - 20, vCenter);
            lblUserInfo.Location = new Point(btnLogout.Left - lblUserInfo.Width - 15, vCenter + 8);
        }

        private static void AddFormRow(GroupBox group, string labelText, TextBox textBox, int y)
        {
            var lbl = new Label
            {
                Text = labelText,
                Location = new Point(15, y + 4),
                AutoSize = true,
                Font = ThemeConfig.MainFont,
                ForeColor = ThemeConfig.TextMain
            };
            textBox.Location = new Point(150, y);
            textBox.Size = new Size(220, 28);
            textBox.Font = ThemeConfig.MainFont;

            group.Controls.Add(lbl);
            group.Controls.Add(textBox);
        }

        private static void AddSummaryRow(GroupBox group, string labelText, Label valLabel, int y, string defaultText, Color? color = null, Font? font = null)
        {
            var lbl = new Label
            {
                Text = labelText,
                Location = new Point(15, y),
                AutoSize = true,
                Font = ThemeConfig.MainFont,
                ForeColor = ThemeConfig.TextMain
            };

            valLabel.Text = defaultText;
            valLabel.Location = new Point(160, y);
            valLabel.AutoSize = true;
            valLabel.Font = font ?? new Font("Segoe UI", 10F, FontStyle.Bold);
            valLabel.ForeColor = color ?? ThemeConfig.TextMain;

            group.Controls.Add(lbl);
            group.Controls.Add(valLabel);
        }

        // ==========================================
        // EVENT WIRING
        // ==========================================
        private void WireEvents()
        {
            cboRoutes.SelectedIndexChanged += async (s, e) => await OnRouteChangedAsync();
            cboVehicles.SelectedIndexChanged += async (s, e) => await OnVehicleChangedAsync();
            dtpDepartureDate.ValueChanged += async (s, e) => await RefreshBookedSeatsAsync();

            seatMapControl.SelectionChanged += (s, e) => CalculateAndRenderTotal();
            cboPromotions.SelectedIndexChanged += (s, e) => CalculateAndRenderTotal();

            btnConfirmBooking.Click += async (s, e) => await ProcessBookingAsync();

            // Real-time error validation setup
            epValidation.ContainerControl = this;
            epValidation.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            txtCustomerName.TextChanged += (s, e) => ValidateName();
            txtCustomerPhone.TextChanged += (s, e) => ValidatePhone();
            txtCustomerEmail.TextChanged += (s, e) => ValidateEmail();
        }

        private void LoadUserData()
        {
            var user = SessionManager.CurrentUser;
            if (user != null)
            {
                lblUserInfo.Text = $"👤 Khách hàng: {user.FullName} ({user.Username})";
                txtCustomerName.Text = user.FullName;
                txtCustomerPhone.Text = user.PhoneNumber ?? "";
                txtCustomerEmail.Text = user.Email ?? "";
            }
            else
            {
                lblUserInfo.Text = "👤 Khách hàng (Guest)";
            }
        }

        private void BtnLogout_Click(object? sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn đăng xuất?",
                "Xác Nhận Đăng Xuất",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                SessionManager.ClearSession();
                this.Close();
            }
        }

        // ==========================================
        // TRIP DATA LOADING LOGIC
        // ==========================================
        private async Task LoadInitialTripDataAsync()
        {
            try
            {
                _routes = await _ticketService.GetActiveRoutesAsync();
                cboRoutes.Items.Clear();

                foreach (var r in _routes)
                {
                    cboRoutes.Items.Add($"{r.RouteCode} - {r.RouteName} ({r.BasePrice:N0}đ)");
                }

                // Load Khuyến mãi giả định
                cboPromotions.Items.Clear();
                cboPromotions.Items.Add("Không áp dụng");
                cboPromotions.Items.Add("KHUYENMAI10 - Giảm 10%");
                cboPromotions.Items.Add("KHUYENMAI20 - Giảm 20%");
                cboPromotions.Items.Add("TRIAN50K - Giảm 50.000 VNĐ");
                cboPromotions.SelectedIndex = 0;

                if (cboRoutes.Items.Count > 0)
                {
                    cboRoutes.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi nạp dữ liệu chuyến xe: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task OnRouteChangedAsync()
        {
            if (cboRoutes.SelectedIndex < 0 || cboRoutes.SelectedIndex >= _routes.Count)
                return;

            _selectedRoute = _routes[cboRoutes.SelectedIndex];
            lblBaseUnitPrice.Text = $"{_selectedRoute.BasePrice:N0} VNĐ";

            _vehicles = await _ticketService.GetVehiclesByRouteAsync(_selectedRoute.Id);
            cboVehicles.Items.Clear();

            if (_vehicles.Count == 0)
            {
                cboVehicles.Items.Add("-- Không có xe phụ trách tuyến này --");
                _selectedVehicle = null;
                seatMapControl.SetSeats(0, null);
            }
            else
            {
                foreach (var v in _vehicles)
                {
                    cboVehicles.Items.Add($"{v.LicensePlate} ({v.VehicleType} - {v.TotalSeats} chỗ)");
                }
                cboVehicles.SelectedIndex = 0;
            }

            CalculateAndRenderTotal();
        }

        private async Task OnVehicleChangedAsync()
        {
            if (cboVehicles.SelectedIndex < 0 || cboVehicles.SelectedIndex >= _vehicles.Count)
            {
                _selectedVehicle = null;
                return;
            }

            _selectedVehicle = _vehicles[cboVehicles.SelectedIndex];
            await RefreshBookedSeatsAsync();
        }

        private async Task RefreshBookedSeatsAsync()
        {
            if (_selectedVehicle == null) return;

            try
            {
                DateTime departureDate = dtpDepartureDate.Value.Date;
                var allTickets = await _ticketService.GetAllTicketsAsync();

                var bookedSeats = allTickets
                    .Where(t => t.VehicleId == _selectedVehicle.Id &&
                                t.DepartureTime.Date == departureDate &&
                                t.TicketStatus != "Cancelled")
                    .Select(t => t.SeatNumber)
                    .ToList();

                seatMapControl.SetSeats(_selectedVehicle.TotalSeats, bookedSeats);
                CalculateAndRenderTotal();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tải ghế đã đặt: {ex.Message}");
            }
        }

        // ==========================================
        // LB-36: LOGIC TÍNH TIỀN & VALIDATION
        // ==========================================
        private void CalculateAndRenderTotal()
        {
            var selectedSeats = seatMapControl.GetSelectedSeats();
            int count = selectedSeats.Count;

            lblSelectedSeatsCount.Text = $"{count} ghế";
            lblSelectedSeatNumbers.Text = count > 0 ? string.Join(", ", selectedSeats) : "Chưa chọn";

            decimal basePrice = _selectedRoute?.BasePrice ?? 0m;
            decimal rawTotal = basePrice * count;

            _currentDiscountAmount = 0m;

            if (count > 0 && cboPromotions.SelectedIndex > 0)
            {
                string promo = cboPromotions.SelectedItem?.ToString() ?? "";
                if (promo.Contains("10%"))
                {
                    _currentDiscountAmount = rawTotal * 0.10m;
                }
                else if (promo.Contains("20%"))
                {
                    _currentDiscountAmount = rawTotal * 0.20m;
                }
                else if (promo.Contains("50.000"))
                {
                    _currentDiscountAmount = Math.Min(rawTotal, 50000m);
                }
            }

            _finalTotalPrice = Math.Max(0m, rawTotal - _currentDiscountAmount);

            lblDiscountAmount.Text = $"-{_currentDiscountAmount:N0} VNĐ";
            lblTotalPrice.Text = $"{_finalTotalPrice:N0} VNĐ";
        }

        private bool ValidateName()
        {
            string name = txtCustomerName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                epValidation.SetError(txtCustomerName, "Vui lòng nhập họ và tên hành khách!");
                return false;
            }
            if (name.Length < 2)
            {
                epValidation.SetError(txtCustomerName, "Họ và tên tối thiểu 2 ký tự!");
                return false;
            }
            epValidation.SetError(txtCustomerName, string.Empty);
            return true;
        }

        private bool ValidatePhone()
        {
            string phone = txtCustomerPhone.Text.Trim();
            if (string.IsNullOrWhiteSpace(phone))
            {
                epValidation.SetError(txtCustomerPhone, "Vui lòng nhập số điện thoại!");
                return false;
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(phone, @"^0\d{9}$"))
            {
                epValidation.SetError(txtCustomerPhone, "Số điện thoại phải bao gồm 10 chữ số (bắt đầu bằng số 0)!");
                return false;
            }
            epValidation.SetError(txtCustomerPhone, string.Empty);
            return true;
        }

        private bool ValidateEmail()
        {
            string email = txtCustomerEmail.Text.Trim();
            if (!string.IsNullOrEmpty(email) && !System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                epValidation.SetError(txtCustomerEmail, "Định dạng email không hợp lệ!");
                return false;
            }
            epValidation.SetError(txtCustomerEmail, string.Empty);
            return true;
        }

        private async Task ProcessBookingAsync()
        {
            try
            {
                bool vName = ValidateName();
                bool vPhone = ValidatePhone();
                bool vEmail = ValidateEmail();

                if (!vName || !vPhone || !vEmail)
                {
                    MessageBox.Show(
                        "Vui lòng kiểm tra lại các thông tin nhập liệu còn thiếu hoặc chưa chính xác!",
                        "Thông Báo Nhập Liệu",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (_selectedRoute == null || _selectedVehicle == null)
                {
                    MessageBox.Show("Vui lòng chọn tuyến xe và phương tiện hợp lệ!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selectedSeats = seatMapControl.GetSelectedSeats();
                if (selectedSeats.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn ít nhất 1 ghế trên sơ đồ xe trước khi đặt vé!", "Chưa Chọn Ghế", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var confirm = MessageBox.Show(
                    $"Xác nhận đặt {selectedSeats.Count} vé ({string.Join(", ", selectedSeats)}) với tổng tiền {_finalTotalPrice:N0} VNĐ?",
                    "Xác Nhận Đặt Vé",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes) return;

                btnConfirmBooking.Enabled = false;
                Cursor = Cursors.WaitCursor;

                _recentlyBookedTickets.Clear();
                decimal unitPrice = selectedSeats.Count > 0 ? _finalTotalPrice / selectedSeats.Count : _selectedRoute.BasePrice;
                DateTime departure = dtpDepartureDate.Value.Date.AddHours(8); // Mặc định 8h sáng

                foreach (var seatNum in selectedSeats)
                {
                    var ticket = new Ticket
                    {
                        CustomerName = txtCustomerName.Text.Trim(),
                        CustomerPhone = txtCustomerPhone.Text.Trim(),
                        RouteId = _selectedRoute.Id,
                        VehicleId = _selectedVehicle.Id,
                        SeatNumber = seatNum,
                        Price = unitPrice,
                        DepartureTime = departure,
                        PaymentStatus = "Paid",
                        TicketStatus = "Booked",
                        AccountId = SessionManager.CurrentUser?.Id
                    };

                    var (success, message, createdTicket) = await _ticketService.CreateTicketAsync(ticket);
                    if (success && createdTicket != null)
                    {
                        _recentlyBookedTickets.Add(createdTicket);
                    }
                    else
                    {
                        MessageBox.Show($"Lỗi đặt ghế {seatNum}: {message}", "Đặt Vé Thất Bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                if (_recentlyBookedTickets.Count > 0)
                {
                    MessageBox.Show(
                        $"🎉 ĐẶT VÉ THÀNH CÔNG!\n\nĐã tạo {_recentlyBookedTickets.Count} vé thành công. Chuẩn bị xuất vé điện tử PDF...",
                        "Thành Công",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    // LB-37: Xuất Vé Điện Tử PDF có nhúng Mã QR
                    TicketPrinterHelper.ShowTicketsPreviewDialog(_recentlyBookedTickets, _selectedRoute, _selectedVehicle);

                    await RefreshBookedSeatsAsync();
                    seatMapControl.ClearSelection();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Đã xảy ra sự cố không mong muốn: {ex.Message}", "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnConfirmBooking.Enabled = true;
                Cursor = Cursors.Default;
            }
        }
    }
}
