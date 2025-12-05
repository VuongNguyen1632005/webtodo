using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models; // Đổi tên namespace nếu cần

namespace WebApplication1.Controllers
{
    [Authorize] // Bắt buộc đăng nhập
    public class BoardController : Controller
    {
        private QL_DUANCANHAN_LITEEntities db = new QL_DUANCANHAN_LITEEntities();

        #region Hàm helper kiểm tra quyền

        private int GetCurrentUserId()
        {
            string emailDangNhap = User.Identity.Name;
            var user = db.TaiKhoans.FirstOrDefault(u => u.DiaChiEmail == emailDangNhap);
            // Nếu không tìm thấy user, trả về -1 để tránh permission bypass
            return user?.MaTaiKhoan ?? -1;
        }

        private string GetUserRole(int maBang, int maTaiKhoan)
        {
            // Nếu userId không hợp lệ, trả về "none"
            if (maTaiKhoan < 0)
                return "none";

            var bang = db.Bangs.Find(maBang);

            // Chủ sở hữu
            if (bang?.MaNguoiSoHuu == maTaiKhoan)
                return "owner";

            // Thành viên được chia sẻ
            var thanhVien = db.ThanhVienBangs
                .FirstOrDefault(tv => tv.MaBang == maBang && tv.MaTaiKhoan == maTaiKhoan);

            return thanhVien?.VaiTro ?? "none";
        }

        private bool CanView(int maBang, int maTaiKhoan)
        {
            var role = GetUserRole(maBang, maTaiKhoan);
            return role != "none";
        }

        private bool CanEdit(int maBang, int maTaiKhoan)
        {
            var role = GetUserRole(maBang, maTaiKhoan);
            return role == "owner" || role == "editor";
        }

        private bool CanManage(int maBang, int maTaiKhoan)
        {
            return GetUserRole(maBang, maTaiKhoan) == "owner";
        }

        #endregion

        [HttpPost]
        public ActionResult CreateBoard(string tenBang, string mauNen)
        {
            try
            {
                //Lấy user hiện tại
                int userId = GetCurrentUserId();
                if (userId == -1) return RedirectToAction("Index", "Home");

                //Tạo bảng mới
                var b = new Bang();
                b.TenBang = tenBang;
                b.MauNen = mauNen;
                b.MaNguoiSoHuu = userId;
                b.NgayTao = DateTime.Now;

                db.Bangs.Add(b);
                db.SaveChanges();

                // Tạo 3 cột mặc định
                var c1 = new Cot() { TenCot = "Cần làm", ThuTu = 0, KichHoat = true, MaBang = b.MaBang };
                var c2 = new Cot() { TenCot = "Đang làm", ThuTu = 1, KichHoat = true, MaBang = b.MaBang };
                var c3 = new Cot() { TenCot = "Đã xong", ThuTu = 2, KichHoat = true, MaBang = b.MaBang };

                db.Cots.Add(c1); db.Cots.Add(c2); db.Cots.Add(c3);
                db.SaveChanges();

                // Tạo xong thì chuyển hướng vào trang chi tiết
                return RedirectToAction("Details", new { id = b.MaBang });
            }
            catch
            {
                return RedirectToAction("Index", "Home");
            }
        }
        public ActionResult Details(int id)
        {
            int userId = GetCurrentUserId();

            // 1. Tìm bảng theo ID
            var board = db.Bangs.FirstOrDefault(b => b.MaBang == id);

            // 2. Bảo mật: Nếu không tìm thấy hoặc không có quyền xem -> Đá về trang chủ
            if (board == null || !CanView(id, userId))
            {
                return RedirectToAction("Index", "Home");
            }

            // Truyền vai trò xuống View để hiển thị/ẩn nút
            ViewBag.UserRole = GetUserRole(id, userId);

            // 3. Trả về View kèm dữ liệu Bảng (EF sẽ tự load Cột và Thẻ liên quan)
            return View(board);
        }

        #region API Chia sẻ bảng

        // API Chia sẻ bảng
        [HttpPost]
        public JsonResult ShareBoard(int maBang, string email, string vaiTro)
        {
            int userId = GetCurrentUserId();

            if (!CanManage(maBang, userId))
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này" });
            }

            var taiKhoan = db.TaiKhoans.FirstOrDefault(t => t.DiaChiEmail == email);
            if (taiKhoan == null)
            {
                // Trả về thông báo chung để tránh user enumeration
                return Json(new { success = false, message = "Không thể mời người dùng này" });
            }

            // Không cho mời chính mình
            if (taiKhoan.MaTaiKhoan == userId)
            {
                return Json(new { success = false, message = "Không thể mời người dùng này" });
            }

            var existing = db.ThanhVienBangs
                .FirstOrDefault(tv => tv.MaBang == maBang && tv.MaTaiKhoan == taiKhoan.MaTaiKhoan);

            if (existing != null)
            {
                return Json(new { success = false, message = "Không thể mời người dùng này" });
            }

            var thanhVien = new ThanhVienBang
            {
                MaBang = maBang,
                MaTaiKhoan = taiKhoan.MaTaiKhoan,
                VaiTro = vaiTro,
                NgayThamGia = DateTime.Now
            };

            db.ThanhVienBangs.Add(thanhVien);
            db.SaveChanges();

            return Json(new { success = true });
        }

        // Lấy danh sách thành viên
        [HttpGet]
        public JsonResult GetBoardMembers(int maBang)
        {
            var bang = db.Bangs.Find(maBang);
            if (bang == null)
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            var members = new List<object>();

            // Thêm chủ sở hữu
            members.Add(new
            {
                maTaiKhoan = bang.TaiKhoan.MaTaiKhoan,
                hoTen = bang.TaiKhoan.HoTen,
                email = bang.TaiKhoan.DiaChiEmail,
                vaiTro = "owner"
            });

            // Thêm các thành viên
            var thanhViens = db.ThanhVienBangs
                .Where(tv => tv.MaBang == maBang)
                .Select(tv => new
                {
                    maTaiKhoan = tv.MaTaiKhoan,
                    hoTen = tv.TaiKhoan.HoTen,
                    email = tv.TaiKhoan.DiaChiEmail,
                    vaiTro = tv.VaiTro
                }).ToList();

            members.AddRange(thanhViens);

            return Json(members, JsonRequestBehavior.AllowGet);
        }

        // Xóa thành viên
        [HttpPost]
        public JsonResult RemoveMember(int maBang, int maTaiKhoan)
        {
            int userId = GetCurrentUserId();

            if (!CanManage(maBang, userId))
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này" });
            }

            var thanhVien = db.ThanhVienBangs
                .FirstOrDefault(tv => tv.MaBang == maBang && tv.MaTaiKhoan == maTaiKhoan);

            if (thanhVien != null)
            {
                db.ThanhVienBangs.Remove(thanhVien);
                db.SaveChanges();
            }

            return Json(new { success = true });
        }

        // Cập nhật vai trò thành viên
        [HttpPost]
        public JsonResult UpdateMemberRole(int maBang, int maTaiKhoan, string vaiTroMoi)
        {
            int userId = GetCurrentUserId();

            if (!CanManage(maBang, userId))
            {
                return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này" });
            }

            var thanhVien = db.ThanhVienBangs
                .FirstOrDefault(tv => tv.MaBang == maBang && tv.MaTaiKhoan == maTaiKhoan);

            if (thanhVien != null)
            {
                thanhVien.VaiTro = vaiTroMoi;
                db.SaveChanges();
            }

            return Json(new { success = true });
        }

        #endregion

        // 1. Thêm thẻ nhanh (Gọi bằng AJAX)
        [HttpPost]
        public JsonResult CreateCard(int maCot, string noiDung)
        {
            try
            {
                // Kiểm tra quyền sửa
                var cot = db.Cots.Find(maCot);
                if (cot == null)
                {
                    return Json(new { success = false, message = "Không thể thực hiện thao tác này" });
                }

                int userId = GetCurrentUserId();
                if (!CanEdit(cot.MaBang, userId))
                {
                    return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này" });
                }

                // Tạo thẻ mới
                var theMoi = new The();
                theMoi.MaCot = maCot;
                theMoi.TieuDe = noiDung;
                theMoi.ThuTu = 0; // Mặc định lên đầu
                theMoi.HanChot = DateTime.Now.AddDays(1);

                db.Thes.Add(theMoi);
                db.SaveChanges();

                // Trả về ID vừa tạo để JS cập nhật giao diện
                return Json(new { success = true, maThe = theMoi.MaThe });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        // 2. Cập nhật vị trí thẻ (Gọi khi Kéo Thả xong)
        [HttpPost]
        public JsonResult UpdateCardPosition(int[] cardIds)
        {
            // cardIds: Là danh sách ID các thẻ trong cột sau khi đã sắp xếp lại
            // Ví dụ: [5, 2, 8] nghĩa là thẻ 5 đứng đầu, rồi đến 2, rồi đến 8

            try
            {
                int thuTu = 0;
                foreach (var id in cardIds)
                {
                    var the = db.Thes.Find(id);
                    if (the != null)
                    {
                        // Kiểm tra quyền sửa
                        int userId = GetCurrentUserId();
                        if (!CanEdit(the.Cot.MaBang, userId))
                        {
                            return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này" });
                        }

                        // Cập nhật lại thứ tự
                        the.ThuTu = thuTu;
                    }
                    thuTu++;
                }
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        // 3. (QUAN TRỌNG) Cập nhật khi kéo sang cột KHÁC
        // 3. SỬA LẠI HÀM MoveCard CHO AN TOÀN
        [HttpPost]
        public JsonResult MoveCard(int maThe, int maCotMoi, int[] cardIds)
        {
            try
            {
                // Kiểm tra quyền sửa
                var cotMoi = db.Cots.Find(maCotMoi);
                if (cotMoi == null)
                {
                    return Json(new { success = false, message = "Không thể thực hiện thao tác này" });
                }

                int userId = GetCurrentUserId();
                if (!CanEdit(cotMoi.MaBang, userId))
                {
                    return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này" });
                }

                // 1. Cập nhật cột mới cho thẻ
                var the = db.Thes.Find(maThe);
                if (the != null)
                {
                    the.MaCot = maCotMoi;
                }

                // 2. Cập nhật thứ tự (QUAN TRỌNG: Phải kiểm tra null để tránh lỗi crash)
                if (cardIds != null)
                {
                    int thuTu = 0;
                    foreach (var id in cardIds)
                    {
                        var t = db.Thes.Find(id);
                        if (t != null)
                        {
                            t.ThuTu = thuTu;
                            // Đảm bảo thẻ cũng được cập nhật cột (phòng hờ)
                            t.MaCot = maCotMoi;
                        }
                        thuTu++;
                    }
                }

                db.SaveChanges();
                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        // 4. Lấy chi tiết thẻ
        // 4. Lấy chi tiết thẻ (Đã thêm Deadline)
        [HttpGet]
        public JsonResult GetCardDetails(int id)
        {
            try
            {
                var the = db.Thes.Find(id);
                if (the != null)
                {
                    return Json(new
                    {
                        success = true,
                        id = the.MaThe,
                        title = the.TieuDe,
                        desc = the.MoTa ?? "",
                        // Chuyển ngày sang định dạng yyyy-MM-dd để gán vào ô input type="date"
                        deadline = the.HanChot.HasValue ? the.HanChot.Value.ToString("yyyy-MM-dd") : ""
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch { }
            return Json(new { success = false }, JsonRequestBehavior.AllowGet);
        }

        // 5. Cập nhật thẻ (Đã thêm Deadline)
        [HttpPost]
        public JsonResult UpdateCardDetails(int id, string title, string desc, string deadline)
        {
            try
            {
                var the = db.Thes.Find(id);
                if (the != null)
                {
                    // Kiểm tra quyền sửa
                    int userId = GetCurrentUserId();
                    if (!CanEdit(the.Cot.MaBang, userId))
                    {
                        return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này" });
                    }

                    the.TieuDe = title;
                    the.MoTa = desc;

                    // Xử lý lưu ngày
                    if (!string.IsNullOrEmpty(deadline))
                        the.HanChot = DateTime.Parse(deadline);
                    else
                        the.HanChot = null; // Người dùng xóa deadline

                    db.SaveChanges();
                    return Json(new { success = true });
                }
            }
            catch { }
            return Json(new { success = false });
        }
        // 6. Xóa thẻ
        [HttpPost]
        public JsonResult DeleteCard(int id)
        {
            try
            {
                var the = db.Thes.Find(id);
                if (the != null)
                {
                    // Kiểm tra quyền sửa
                    int userId = GetCurrentUserId();
                    if (!CanEdit(the.Cot.MaBang, userId))
                    {
                        return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này" });
                    }

                    db.Thes.Remove(the);
                    db.SaveChanges();
                    return Json(new { success = true });
                }
            }
            catch { }
            return Json(new { success = false });
        }

        // 7. Tạo cột mới
        [HttpPost]
        public JsonResult CreateColumn(int maBang, string tenCot)
        {
            try
            {
                // Kiểm tra quyền sửa
                int userId = GetCurrentUserId();
                if (!CanEdit(maBang, userId))
                {
                    return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này" });
                }

                var cotMoi = new Cot();
                cotMoi.MaBang = maBang;
                cotMoi.TenCot = tenCot;

                // Tính thứ tự: Cho nằm cuối cùng
                var soLuongCot = db.Cots.Count(c => c.MaBang == maBang);
                cotMoi.ThuTu = soLuongCot;

                cotMoi.KichHoat = true; // Mặc định là hiện

                db.Cots.Add(cotMoi);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        // 8. Cập nhật vị trí Cột (Kéo thả cột)
        [HttpPost]
        public JsonResult UpdateColumnPosition(int[] columnIds)
        {
            try
            {
                int thuTu = 0;
                foreach (var id in columnIds)
                {
                    var cot = db.Cots.Find(id);
                    if (cot != null)
                    {
                        // Kiểm tra quyền sửa
                        int userId = GetCurrentUserId();
                        if (!CanEdit(cot.MaBang, userId))
                        {
                            return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này" });
                        }

                        cot.ThuTu = thuTu;
                    }
                    thuTu++;
                }
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        // 9. Cập nhật trạng thái Hoàn thành (Checkbox)
        [HttpPost]
        public JsonResult UpdateCardCompletion(int id, bool status)
        {
            try
            {
                var the = db.Thes.Find(id);
                if (the != null)
                {
                    // Kiểm tra quyền sửa
                    int userId = GetCurrentUserId();
                    if (!CanEdit(the.Cot.MaBang, userId))
                    {
                        return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này" });
                    }

                    the.DaHoanThanh = status; // Lưu trạng thái (true/false)
                    db.SaveChanges();
                    return Json(new { success = true });
                }
            }
            catch { }
            return Json(new { success = false });
        }



    }
}