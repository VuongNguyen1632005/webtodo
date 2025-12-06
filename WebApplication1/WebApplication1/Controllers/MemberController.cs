using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class MemberController : Controller
    {
        private QL_DUANCANHAN_LITEEntities1 db = new QL_DUANCANHAN_LITEEntities1();

        // HÀM HELPER (Để kiểm tra quyền)
        private int GetCurrentUserId()
        {
            string email = User.Identity.Name;
            var user = db.TaiKhoans.FirstOrDefault(u => u.DiaChiEmail == email);
            return user?.MaTaiKhoan ?? -1;
        }

        private bool CanManage(int maBang, int userId)
        {
            var bang = db.Bangs.Find(maBang);
            return bang != null && bang.MaNguoiSoHuu == userId;
        }

        //CÁC CHỨC NĂNG QUẢN LÝ THÀNH VIÊN

        // TRANG QUẢN LÝ THÀNH VIÊN 
        public ActionResult Index(int maBang)
        {
            int userId = GetCurrentUserId();

            // Kiểm tra quyền chủ sở hữu
            if (!CanManage(maBang, userId))
            {
                // Nếu không phải chủ, đá về trang chi tiết bảng
                return RedirectToAction("Details", "Board", new { id = maBang });
            }

            var bang = db.Bangs.Find(maBang);
            return View(bang);
        }

        // MỜI THÀNH VIÊN
        [HttpPost]
        public ActionResult Add(int maBang, string email, string vaiTro)
        {
            int userId = GetCurrentUserId();
            if (CanManage(maBang, userId))
            {
                var userMoi = db.TaiKhoans.FirstOrDefault(t => t.DiaChiEmail == email);

                // Kiểm tra hợp lệ: Có user, không phải chính mình
                if (userMoi != null && userMoi.MaTaiKhoan != userId)
                {
                    // Kiểm tra đã có trong bảng chưa
                    var check = db.ThanhVienBangs.Any(x => x.MaBang == maBang && x.MaTaiKhoan == userMoi.MaTaiKhoan);

                    if (!check)
                    {
                        var tv = new ThanhVienBang();
                        tv.MaBang = maBang;
                        tv.MaTaiKhoan = userMoi.MaTaiKhoan;
                        tv.VaiTro = vaiTro;
                        tv.NgayThamGia = DateTime.Now;

                        db.ThanhVienBangs.Add(tv);
                        db.SaveChanges();
                        TempData["Message"] = "Mời thành công: " + email;
                    }
                    else
                    {
                        TempData["Error"] = "Thành viên này đã có trong bảng rồi!";
                    }
                }
                else
                {
                    TempData["Error"] = "Email không tồn tại hoặc không hợp lệ!";
                }
            }
            // Quay lại trang danh sách thành viên
            return RedirectToAction("Index", new { maBang = maBang });
        }

        // XÓA THÀNH VIÊN
        [HttpPost]
        public ActionResult Remove(int maBang, int maTaiKhoan)
        {
            int userId = GetCurrentUserId();
            if (CanManage(maBang, userId))
            {
                var tv = db.ThanhVienBangs.FirstOrDefault(x => x.MaBang == maBang && x.MaTaiKhoan == maTaiKhoan);
                if (tv != null)
                {
                    db.ThanhVienBangs.Remove(tv);
                    db.SaveChanges();
                    TempData["Message"] = "Đã xóa thành viên khỏi bảng.";
                }
            }
            return RedirectToAction("Index", new { maBang = maBang });
        }

        // ĐỔI QUYỀN 
        [HttpPost]
        public ActionResult UpdateRole(int maBang, int maTaiKhoan, string vaiTro)
        {
            int userId = GetCurrentUserId();
            if (CanManage(maBang, userId))
            {
                var tv = db.ThanhVienBangs.FirstOrDefault(x => x.MaBang == maBang && x.MaTaiKhoan == maTaiKhoan);
                if (tv != null)
                {
                    tv.VaiTro = vaiTro;
                    db.SaveChanges();
                    TempData["Message"] = "Đã cập nhật quyền hạn.";
                }
            }
            return RedirectToAction("Index", new { maBang = maBang });
        }

        // RỜI BẢNG (Cho thành viên tự thoát)
        [HttpPost]
        public ActionResult Leave(int maBang)
        {
            int userId = GetCurrentUserId();

            // Chủ không được rời
            if (!CanManage(maBang, userId))
            {
                var tv = db.ThanhVienBangs.FirstOrDefault(x => x.MaBang == maBang && x.MaTaiKhoan == userId);
                if (tv != null)
                {
                    db.ThanhVienBangs.Remove(tv);
                    db.SaveChanges();
                }
            }
            // Rời xong thì về trang chủ
            return RedirectToAction("Index", "Home");
        }
    }
}