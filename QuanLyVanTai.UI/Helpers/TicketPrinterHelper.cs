using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using Core.Configs;
using QRCoder;
using QuanLyVanTai.DAL.Models;

namespace QuanLyVanTai.UI.Helpers
{
    public static class TicketPrinterHelper
    {
        public static Bitmap GenerateQrCodeBitmap(string content, int pixelsPerModule = 6)
        {
            using QRCodeGenerator qrGenerator = new QRCodeGenerator();
            using QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            using QRCode qrCode = new QRCode(qrCodeData);
            return qrCode.GetGraphic(pixelsPerModule, Color.Black, Color.White, drawQuietZones: true);
        }

        public static void ShowTicketPreviewDialog(Ticket ticket, Route? route = null, Vehicle? vehicle = null)
        {
            ShowTicketsPreviewDialog(new List<Ticket> { ticket }, route, vehicle);
        }

        public static void ShowTicketsPreviewDialog(List<Ticket> tickets, Route? route = null, Vehicle? vehicle = null)
        {
            if (tickets == null || tickets.Count == 0) return;

            int currentTicketIndex = 0;

            using PrintDocument printDoc = new PrintDocument();
            printDoc.DocumentName = $"Ve_Dien_Tu_{tickets[0].TicketCode}";

            // Thiết lập khổ giấy chuẩn A5 / A4
            printDoc.DefaultPageSettings.PaperSize = new PaperSize("TicketPaper", 600, 850);
            printDoc.DefaultPageSettings.Margins = new Margins(30, 30, 30, 30);

            printDoc.PrintPage += (s, e) =>
            {
                if (currentTicketIndex >= tickets.Count)
                {
                    e.HasMorePages = false;
                    return;
                }

                var ticket = tickets[currentTicketIndex];
                DrawSingleTicketPage(e.Graphics!, e.MarginBounds, ticket, route ?? ticket.Route, vehicle ?? ticket.Vehicle);

                currentTicketIndex++;
                e.HasMorePages = currentTicketIndex < tickets.Count;
            };

            using PrintPreviewDialog previewDialog = new PrintPreviewDialog();
            previewDialog.Document = printDoc;
            previewDialog.Width = 800;
            previewDialog.Height = 900;
            previewDialog.StartPosition = FormStartPosition.CenterScreen;
            previewDialog.Text = "Xem Trước & Xuất Vé Điện Tử (PDF)";

            previewDialog.ShowDialog();
        }

        private static void DrawSingleTicketPage(Graphics g, Rectangle bounds, Ticket ticket, Route? route, Vehicle? vehicle)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Khung viền ngoài của vé điện tử
            int x = bounds.X;
            int y = bounds.Y;
            int w = bounds.Width;
            int h = bounds.Height;

            var outerRect = new Rectangle(x, y, w, h);
            using (var borderPen = new Pen(ThemeConfig.PrimaryColor, 2))
            using (var bgBrush = new SolidBrush(Color.White))
            {
                g.FillRectangle(bgBrush, outerRect);
                g.DrawRectangle(borderPen, outerRect);
            }

            // 1. Banner Header
            var headerRect = new Rectangle(x, y, w, 80);
            using (var headerBrush = new SolidBrush(ThemeConfig.PrimaryColor))
            {
                g.FillRectangle(headerBrush, headerRect);
            }

            using (var titleFont = new Font("Segoe UI", 16, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 10, FontStyle.Regular))
            using (var whiteBrush = new SolidBrush(Color.White))
            {
                g.DrawString("CÔNG TY VẬN TẢI HÀNH KHÁCH CÔNG CỘNG", titleFont, whiteBrush, x + 20, y + 15);
                g.DrawString("VÉ XE ĐIỆN TỬ & XÁC NHẬN ĐẶT CHỖ (E-TICKET)", subFont, whiteBrush, x + 20, y + 48);
            }

            // 2. Nhúng Mã QR Code ở góc trên bên phải
            string qrPayload = $"MÃ VÉ: {ticket.TicketCode}\nKHÁCH HÀNG: {ticket.CustomerName}\nSĐT: {ticket.CustomerPhone}\nGHẾ: {ticket.SeatNumber}\nKHỞI HÀNH: {ticket.DepartureTime:dd/MM/yyyy HH:mm}\nGIÁ: {ticket.Price:N0} VNĐ";
            using Bitmap qrBitmap = GenerateQrCodeBitmap(qrPayload);
            int qrSize = 100;
            int qrX = x + w - qrSize - 20;
            int qrY = y + 100;

            g.DrawImage(qrBitmap, qrX, qrY, qrSize, qrSize);

            using (var qrCaptionFont = new Font("Segoe UI", 8, FontStyle.Italic))
            using (var grayBrush = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                g.DrawString("Quét mã QR để kiểm tra", qrCaptionFont, grayBrush, qrX - 5, qrY + qrSize + 4);
            }

            // 3. Thông tin Mã vé & Ngày xuất
            int curY = y + 105;
            using (var codeFont = new Font("Segoe UI", 13, FontStyle.Bold))
            using (var labelFont = new Font("Segoe UI", 10, FontStyle.Bold))
            using (var valFont = new Font("Segoe UI", 10, FontStyle.Regular))
            using (var primaryBrush = new SolidBrush(ThemeConfig.PrimaryColor))
            using (var darkBrush = new SolidBrush(ThemeConfig.TextMain))
            {
                g.DrawString($"MÃ VÉ: {ticket.TicketCode}", codeFont, primaryBrush, x + 20, curY); curY += 32;
                g.DrawString($"Ngày lập vé: {DateTime.Now:dd/MM/yyyy HH:mm}", valFont, darkBrush, x + 20, curY); curY += 35;

                // Đường phân cách nét đứt
                DrawDashedLine(g, x + 20, curY, x + w - 20, curY); curY += 20;

                // 4. Thông tin Hành khách
                g.DrawString("1. THÔNG TIN HÀNH KHÁCH", labelFont, primaryBrush, x + 20, curY); curY += 25;
                DrawFieldRow(g, "Họ và tên:", ticket.CustomerName, x + 30, ref curY, labelFont, valFont, darkBrush);
                DrawFieldRow(g, "Số điện thoại:", ticket.CustomerPhone, x + 30, ref curY, labelFont, valFont, darkBrush);
                DrawFieldRow(g, "Trạng thái thanh toán:", ticket.PaymentStatus == "Paid" ? "ĐÃ THANH TOÁN" : ticket.PaymentStatus, x + 30, ref curY, labelFont, valFont, darkBrush);

                curY += 10;
                DrawDashedLine(g, x + 20, curY, x + w - 20, curY); curY += 20;

                // 5. Thông tin Chuyến xe & Hành trình
                g.DrawString("2. THÔNG TIN CHUYẾN XE", labelFont, primaryBrush, x + 20, curY); curY += 25;

                string routeName = route?.RouteName ?? "Tuyến xe liên tỉnh";
                string routeCode = route?.RouteCode ?? "";
                string vehiclePlate = vehicle?.LicensePlate ?? "Chưa phân công";
                string vehicleType = vehicle?.VehicleType ?? "Xe khách";

                DrawFieldRow(g, "Tuyến đường:", $"{routeCode} - {routeName}", x + 30, ref curY, labelFont, valFont, darkBrush);
                DrawFieldRow(g, "Thời gian khởi hành:", ticket.DepartureTime.ToString("HH:mm - dddd, dd/MM/yyyy"), x + 30, ref curY, labelFont, valFont, darkBrush);
                DrawFieldRow(g, "Số ghế / Giường:", ticket.SeatNumber, x + 30, ref curY, labelFont, valFont, darkBrush);
                DrawFieldRow(g, "Phương tiện:", $"{vehiclePlate} ({vehicleType})", x + 30, ref curY, labelFont, valFont, darkBrush);
                DrawFieldRow(g, "Giá vé thanh toán:", $"{ticket.Price:N0} VNĐ", x + 30, ref curY, labelFont, valFont, darkBrush);

                curY += 15;
                DrawDashedLine(g, x + 20, curY, x + w - 20, curY); curY += 25;

                // 6. Lời cảm ơn & Lưu ý
                using (var footerTitleFont = new Font("Segoe UI", 9, FontStyle.Bold))
                using (var footerFont = new Font("Segoe UI", 8.5F, FontStyle.Italic))
                {
                    g.DrawString("LƯU Ý DÀNH CHO HÀNH KHÁCH:", footerTitleFont, darkBrush, x + 20, curY); curY += 20;
                    g.DrawString("- Vui lòng có mặt tại bến xe/điểm đón trước thời gian khởi hành 15-30 phút.", footerFont, darkBrush, x + 20, curY); curY += 18;
                    g.DrawString("- Xuất trình Vé điện tử (hoặc ảnh chụp màn hình chứa Mã QR) cho phụ xe khi lên xe.", footerFont, darkBrush, x + 20, curY); curY += 18;
                    g.DrawString("- Tổng đài hỗ trợ & Chăm sóc khách hàng: 1900 123 456 (24/7).", footerFont, darkBrush, x + 20, curY); curY += 30;

                    g.DrawString("KÍNH CHÚC QUÝ KHÁCH MỘT CHUYẾN ĐI AN TOÀN VÀ THƯỢNG LỘ BÌNH AN!", labelFont, primaryBrush, x + 20, curY);
                }
            }
        }

        private static void DrawFieldRow(Graphics g, string label, string value, int x, ref int y, Font labelFont, Font valFont, Brush brush)
        {
            g.DrawString(label, labelFont, brush, x, y);
            g.DrawString(value, valFont, brush, x + 160, y);
            y += 24;
        }

        private static void DrawDashedLine(Graphics g, int x1, int y1, int x2, int y2)
        {
            using var pen = new Pen(Color.FromArgb(203, 213, 225), 1.5f);
            pen.DashStyle = DashStyle.Dash;
            g.DrawLine(pen, x1, y1, x2, y2);
        }
    }
}
