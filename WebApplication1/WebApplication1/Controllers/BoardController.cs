using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize] // Bắt buộc đăng nhập mới được dùng Controller này
    public class BoardController : Controller
    {
        private QL_DUANCANHAN_LITEEntities db = new QL_DUANCANHAN_LITEEntities();

        #region CÁC HÀM HELPER KIỂM TRA QUYỀN (PRIVATE)

        private int GetCurrentUserId()
        {
            string emailDangNhap = User.Identity.Name;
            var user = db.TaiKhoans.FirstOrDefault(u => u.DiaChiEmail == emailDangNhap);
            return user?.MaTaiKhoan ?? -1;
        }

        // Lấy vai trò: "owner", "editor", "viewer", hoặc "none"
        private string GetUserRole(int maBang, int maTaiKhoan)
        {
            if (maTaiKhoan < 0) return "none";

            var bang = db.Bangs.Find(maBang);
            if (bang == null) return "none";

            // Nếu là chủ sở hữu
            if (bang.MaNguoiSoHuu == maTaiKhoan) return "owner";

            // Kiểm tra trong bảng thành viên chia sẻ
            var thanhVien = db.ThanhVienBangs
                .FirstOrDefault(tv => tv.MaBang == maBang && tv.MaTaiKhoan == maTaiKhoan);

            return thanhVien?.VaiTro ?? "none";
        }

        // Quyền xem: Bất kỳ ai trong bảng
        private bool CanView(int maBang, int maTaiKhoan)
        {
            return GetUserRole(maBang, maTaiKhoan) != "none";
        }

        // Quyền sửa: Owner hoặc Editor
        private bool CanEdit(int maBang, int maTaiKhoan)
        {
            var role = GetUserRole(maBang, maTaiKhoan);
            return role == "owner" || role == "editor";
        }

        // Quyền quản trị (Xóa bảng, chia sẻ): Chỉ Owner
        private bool CanManage(int maBang, int maTaiKhoan)
        {
            return GetUserRole(maBang, maTaiKhoan) == "owner";
        }

        #endregion

        #region  QUẢN LÝ BẢNG (DETAILS, CREATE, DELETE, UPDATE COLOR)

        // Trang chính Kanban
        public ActionResult Details(int id)
        {
            int userId = GetCurrentUserId();
            var board = db.Bangs.FirstOrDefault(b => b.MaBang == id);

            // Bảo mật: Nếu không có quyền -> Đá về trang chủ
            if (board == null || !CanView(id, userId))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.UserRole = GetUserRole(id, userId);
            return View(board);
        }

        // Tạo bảng mới
        [HttpPost]
        public JsonResult Create(string tenBang, string mauNen)
        {
            try
            {
                int userId = GetCurrentUserId();
                if (userId == -1) return Json(new { success = false, message = "Lỗi phiên đăng nhập" });

                var bang = new Bang
                {
                    TenBang = tenBang,
                    MauNen = mauNen,
                    MaNguoiSoHuu = userId
                };

                // Tạo 3 cột mặc định
                bang.Cots = new List<Cot>
                {
                    new Cot { TenCot = "Cần làm", ThuTu = 0, KichHoat = true },
                    new Cot { TenCot = "Đang thực hiện", ThuTu = 1, KichHoat = true },
                    new Cot { TenCot = "Đã xong", ThuTu = 2, KichHoat = true }
                };

                db.Bangs.Add(bang);
                db.SaveChanges();

                return Json(new { success = true, id = bang.MaBang });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Xóa bảng (Chỉ Owner)
        [HttpPost]
        public JsonResult DeleteBoard(int maBang)
        {
            try
            {
                int userId = GetCurrentUserId();
                if (!CanManage(maBang, userId)) return Json(new { success = false, message = "Chỉ chủ sở hữu mới được xóa bảng" });

                var bang = db.Bangs.Find(maBang);
                if (bang != null)
                {
                    // Xóa dữ liệu liên quan (Thủ công để tránh lỗi khóa ngoại nếu DB chưa set Cascade)
                    var tv = db.ThanhVienBangs.Where(x => x.MaBang == maBang);
                    db.ThanhVienBangs.RemoveRange(tv);

                    var cots = db.Cots.Where(c => c.MaBang == maBang);
                    foreach (var c in cots)
                    {
                        var thes = db.Thes.Where(t => t.MaCot == c.MaCot);
                        db.Thes.RemoveRange(thes);
                    }
                    db.Cots.RemoveRange(cots);

                    db.Bangs.Remove(bang);
                    db.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Không tìm thấy bảng" });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // Đổi màu nền
        [HttpPost]
        public JsonResult UpdateColor(int maBang, string mauNen)
        {
            try
            {
                int userId = GetCurrentUserId();
                if (!CanManage(maBang, userId)) return Json(new { success = false });

                var bang = db.Bangs.Find(maBang);
                if (bang != null)
                {
                    bang.MauNen = mauNen;
                    db.SaveChanges();
                    return Json(new { success = true });
                }
            }
            catch { }
            return Json(new { success = false });
        }

        #endregion

        #region CHIA SẺ & PHÂN QUYỀN (SHARE, MEMBERS)

        // Trang quản lý thành viên 
        public ActionResult Share(int id)
        {
            int userId = GetCurrentUserId();
            if (!CanManage(id, userId)) return RedirectToAction("Details", new { id = id });

            var bang = db.Bangs.Find(id);
            if (bang == null) return HttpNotFound();

            return View(bang);
        }

        // API: Mời thành viên
        [HttpPost]
        public JsonResult ShareBoard(int maBang, string email, string vaiTro)
        {
            int userId = GetCurrentUserId();
            if (!CanManage(maBang, userId)) return Json(new { success = false, message = "Không có quyền" });

            var taiKhoan = db.TaiKhoans.FirstOrDefault(t => t.DiaChiEmail == email);
            if (taiKhoan == null || taiKhoan.MaTaiKhoan == userId)
                return Json(new { success = false, message = "Email không hợp lệ hoặc đã là chủ sở hữu" });

            var existing = db.ThanhVienBangs.FirstOrDefault(tv => tv.MaBang == maBang && tv.MaTaiKhoan == taiKhoan.MaTaiKhoan);
            if (existing != null) return Json(new { success = false, message = "Người này đã là thành viên rồi" });

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

        // API: Rời khỏi bảng (Cho thành viên)
        [HttpPost]
        public JsonResult LeaveBoard(int maBang)
        {
            try
            {
                int userId = GetCurrentUserId();
                if (CanManage(maBang, userId)) return Json(new { success = false, message = "Chủ sở hữu không thể rời bảng." });

                var thanhVien = db.ThanhVienBangs.FirstOrDefault(tv => tv.MaBang == maBang && tv.MaTaiKhoan == userId);
                if (thanhVien != null)
                {
                    db.ThanhVienBangs.Remove(thanhVien);
                    db.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // Lấy danh sách thành viên
        [HttpGet]
        public JsonResult GetBoardMembers(int maBang)
        {
            var bang = db.Bangs.Find(maBang);
            if (bang == null) return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            var members = new List<object>();
            // Chủ sở hữu
            members.Add(new { maTaiKhoan = bang.TaiKhoan.MaTaiKhoan, hoTen = bang.TaiKhoan.HoTen, email = bang.TaiKhoan.DiaChiEmail, vaiTro = "owner" });

            // Thành viên
            var thanhViens = db.ThanhVienBangs.Where(tv => tv.MaBang == maBang)
                .Select(tv => new { maTaiKhoan = tv.MaTaiKhoan, hoTen = tv.TaiKhoan.HoTen, email = tv.TaiKhoan.DiaChiEmail, vaiTro = tv.VaiTro }).ToList();

            members.AddRange(thanhViens);
            return Json(members, JsonRequestBehavior.AllowGet);
        }

        // Xóa thành viên (Kick)
        [HttpPost]
        public JsonResult RemoveMember(int maBang, int maTaiKhoan)
        {
            int userId = GetCurrentUserId();
            if (!CanManage(maBang, userId)) return Json(new { success = false });

            var thanhVien = db.ThanhVienBangs.FirstOrDefault(tv => tv.MaBang == maBang && tv.MaTaiKhoan == maTaiKhoan);
            if (thanhVien != null)
            {
                db.ThanhVienBangs.Remove(thanhVien);
                db.SaveChanges();
            }
            return Json(new { success = true });
        }

        // Đổi quyền thành viên
        [HttpPost]
        public JsonResult UpdateMemberRole(int maBang, int maTaiKhoan, string vaiTroMoi)
        {
            int userId = GetCurrentUserId();
            if (!CanManage(maBang, userId)) return Json(new { success = false });

            var thanhVien = db.ThanhVienBangs.FirstOrDefault(tv => tv.MaBang == maBang && tv.MaTaiKhoan == maTaiKhoan);
            if (thanhVien != null)
            {
                thanhVien.VaiTro = vaiTroMoi;
                db.SaveChanges();
            }
            return Json(new { success = true });
        }

        #endregion

        #region QUẢN LÝ CỘT (DANH SÁCH)

        [HttpPost]
        public JsonResult CreateColumn(int maBang, string tenCot)
        {
            int userId = GetCurrentUserId();
            if (!CanEdit(maBang, userId)) return Json(new { success = false });

            var cot = new Cot
            {
                MaBang = maBang,
                TenCot = tenCot,
                ThuTu = db.Cots.Count(c => c.MaBang == maBang),
                KichHoat = true
            };
            db.Cots.Add(cot);
            db.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult DeleteColumn(int maCot)
        {
            try
            {
                var cot = db.Cots.Find(maCot);
                if (cot == null) return Json(new { success = false });

                int userId = GetCurrentUserId();
                if (!CanEdit(cot.MaBang, userId)) return Json(new { success = false, message = "Không có quyền xóa" });

                var thes = db.Thes.Where(t => t.MaCot == maCot);
                db.Thes.RemoveRange(thes);
                db.Cots.Remove(cot);
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        public JsonResult UpdateColumnPosition(int[] columnIds)
        {
            if (columnIds == null) return Json(new { success = false });
            int order = 0;
            foreach (var id in columnIds)
            {
                var cot = db.Cots.Find(id);
                if (cot != null) cot.ThuTu = order++;
            }
            db.SaveChanges();
            return Json(new { success = true });
        }

        #endregion

        #region QUẢN LÝ THẺ (CARDS)

        [HttpPost]
        public JsonResult CreateCard(int maCot, string noiDung)
        {
            var cot = db.Cots.Find(maCot);
            if (cot == null) return Json(new { success = false });
            if (!CanEdit(cot.MaBang, GetCurrentUserId())) return Json(new { success = false });

            var the = new The
            {
                MaCot = maCot,
                TieuDe = noiDung,
                ThuTu = 0,
                HanChot = DateTime.Now.AddDays(1)
            };
            db.Thes.Add(the);
            db.SaveChanges();
            return Json(new { success = true, maThe = the.MaThe });
        }

        [HttpGet]
        public JsonResult GetCardDetails(int id)
        {
            var the = db.Thes.Find(id);
            if (the == null) return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            return Json(new
            {
                success = true,
                id = the.MaThe,
                title = the.TieuDe,
                desc = the.MoTa ?? "",
                deadline = the.HanChot.HasValue ? the.HanChot.Value.ToString("yyyy-MM-dd") : ""
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateCardDetails(int id, string title, string desc, string deadline)
        {
            var the = db.Thes.Find(id);
            if (the == null) return Json(new { success = false });
            if (!CanEdit(the.Cot.MaBang, GetCurrentUserId())) return Json(new { success = false });

            the.TieuDe = title;
            the.MoTa = desc;
            if (!string.IsNullOrEmpty(deadline)) the.HanChot = DateTime.Parse(deadline);
            else the.HanChot = null;

            db.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult MoveCard(int maThe, int maCotMoi, int[] cardIds)
        {
            var cotMoi = db.Cots.Find(maCotMoi);
            if (cotMoi == null) return Json(new { success = false });
            if (!CanEdit(cotMoi.MaBang, GetCurrentUserId())) return Json(new { success = false });

            // Đổi cột
            var the = db.Thes.Find(maThe);
            if (the != null) the.MaCot = maCotMoi;

            // Sắp xếp lại
            if (cardIds != null)
            {
                int order = 0;
                foreach (var id in cardIds)
                {
                    var t = db.Thes.Find(id);
                    if (t != null)
                    {
                        t.ThuTu = order++;
                        t.MaCot = maCotMoi; // Đảm bảo chắc chắn
                    }
                }
            }
            db.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult UpdateCardCompletion(int id, bool status)
        {
            var the = db.Thes.Find(id);
            if (the == null) return Json(new { success = false });
            if (!CanEdit(the.Cot.MaBang, GetCurrentUserId())) return Json(new { success = false });

            the.DaHoanThanh = status;
            db.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult DeleteCard(int id)
        {
            var the = db.Thes.Find(id);
            if (the == null) return Json(new { success = false });
            if (!CanEdit(the.Cot.MaBang, GetCurrentUserId())) return Json(new { success = false });

            db.Thes.Remove(the);
            db.SaveChanges();
            return Json(new { success = true });
        }

        #endregion
    }
}