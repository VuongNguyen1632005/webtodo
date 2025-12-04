using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class BoardController : Controller
    {
        private QL_DUANCANHAN_LITEEntities db = new QL_DUANCANHAN_LITEEntities();

        // XEM CHI TIẾT BẢNG ---
        public ActionResult Details(int id)
        {
            string email = User.Identity.Name;
            var user = db.TaiKhoans.Where(u => u.DiaChiEmail == email).FirstOrDefault();
            var board = db.Bangs.Where(b => b.MaBang == id).FirstOrDefault();

            // Kiểm tra quyền cơ bản (nếu ko phải chủ, ko phải thành viên -> về Home)
            var tv = db.ThanhVienBangs.Where(x => x.MaBang == id && x.MaTaiKhoan == user.MaTaiKhoan).FirstOrDefault();

            if (board.MaNguoiSoHuu != user.MaTaiKhoan && tv == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Gán quyền để View dùng
            if (board.MaNguoiSoHuu == user.MaTaiKhoan) ViewBag.UserRole = "owner";
            else ViewBag.UserRole = tv.VaiTro;

            return View(board);
        }

        // TẠO BẢNG 
        [HttpPost]
        public ActionResult Create(string tenBang, string mauNen)
        {
            string email = User.Identity.Name;
            var user = db.TaiKhoans.Where(u => u.DiaChiEmail == email).FirstOrDefault();

            Bang b = new Bang();
            b.TenBang = tenBang;
            b.MauNen = mauNen;
            b.MaNguoiSoHuu = user.MaTaiKhoan;

            Cot c1 = new Cot() { TenCot = "Cần làm", ThuTu = 0, KichHoat = true, MaBang = b.MaBang };
            Cot c2 = new Cot() { TenCot = "Đang làm", ThuTu = 1, KichHoat = true, MaBang = b.MaBang };
            Cot c3 = new Cot() { TenCot = "Đã xong", ThuTu = 2, KichHoat = true, MaBang = b.MaBang };

            db.Bangs.Add(b);
            db.SaveChanges();

            c1.MaBang = b.MaBang; c2.MaBang = b.MaBang; c3.MaBang = b.MaBang;
            db.Cots.Add(c1); db.Cots.Add(c2); db.Cots.Add(c3);
            db.SaveChanges();

            return RedirectToAction("Details", new { id = b.MaBang });
        }

        //TẠO CỘT MỚI
        [HttpPost]
        public ActionResult CreateColumn(int maBang, string tenCot)
        {
            Cot c = new Cot();
            c.MaBang = maBang;
            c.TenCot = tenCot;
            c.KichHoat = true;
            c.ThuTu = db.Cots.Where(x => x.MaBang == maBang).Count();

            db.Cots.Add(c);
            db.SaveChanges();

            return RedirectToAction("Details", new { id = maBang });
        }

        //TẠO THẺ MỚI
        [HttpPost]
        public ActionResult CreateCard(int maCot, string noiDung, int maBang)
        {
            The t = new The();
            t.MaCot = maCot;
            t.TieuDe = noiDung;
            t.ThuTu = 0;
            t.HanChot = DateTime.Now.AddDays(1);
            t.DaHoanThanh = false;

            db.Thes.Add(t);
            db.SaveChanges();

            return RedirectToAction("Details", new { id = maBang });
        }

        //XÓA THẺ 
        [HttpPost] 
        public ActionResult DeleteCard(int id, int maBang)
        {
            var t = db.Thes.Where(x => x.MaThe == id).FirstOrDefault();
            if (t != null)
            {
                db.Thes.Remove(t);
                db.SaveChanges();
            }
            return RedirectToAction("Details", new { id = maBang });
        }

        //CHECK HOÀN THÀNH 
        [HttpPost]
        public ActionResult ToggleComplete(int id, int maBang)
        {
            var t = db.Thes.Where(x => x.MaThe == id).FirstOrDefault();
            if (t != null)
            {
                if (t.DaHoanThanh == true) t.DaHoanThanh = false;
                else t.DaHoanThanh = true;

                db.SaveChanges();
            }
            return RedirectToAction("Details", new { id = maBang });
        }

        // DI CHUYỂN THẺ ---
        // Vì không dùng JSON/AJAX, ta làm tính năng "Chuyển sang cột bên cạnh"
        [HttpPost]
        public ActionResult MoveCardNext(int maThe, int maCotMoi, int maBang)
        {
            var t = db.Thes.Where(x => x.MaThe == maThe).FirstOrDefault();
            if (t != null)
            {
                t.MaCot = maCotMoi;
                db.SaveChanges();
            }
            return RedirectToAction("Details", new { id = maBang });
        }

        // --- 8. XÓA BẢNG ---
        [HttpPost]
        public ActionResult DeleteBoard(int maBang)
        {
            var b = db.Bangs.Where(x => x.MaBang == maBang).FirstOrDefault();
            if (b != null)
            {
                // Xóa thủ công từng bảng con (nếu SQL chưa set Cascade)
                var tv = db.ThanhVienBangs.Where(x => x.MaBang == maBang).ToList();
                db.ThanhVienBangs.RemoveRange(tv);

                var cots = db.Cots.Where(x => x.MaBang == maBang).ToList();
                foreach (var c in cots)
                {
                    var thes = db.Thes.Where(x => x.MaCot == c.MaCot).ToList();
                    db.Thes.RemoveRange(thes);
                }
                db.Cots.RemoveRange(cots);

                db.Bangs.Remove(b);
                db.SaveChanges();
            }
            // Xóa xong về trang chủ
            return RedirectToAction("Index", "Home");
        }

        // --- 9. TRANG CHIA SẺ (VIEW RIÊNG) ---
        public ActionResult Share(int id)
        {
            // Kiểm tra quyền chủ sở hữu
            string email = User.Identity.Name;
            var user = db.TaiKhoans.Where(u => u.DiaChiEmail == email).FirstOrDefault();
            var bang = db.Bangs.Where(b => b.MaBang == id).FirstOrDefault();

            if (bang.MaNguoiSoHuu != user.MaTaiKhoan)
            {
                return RedirectToAction("Details", new { id = id });
            }

            return View(bang);
        }

        // --- 10. XỬ LÝ MỜI THÀNH VIÊN (FORM POST) ---
        [HttpPost]
        public ActionResult AddMember(int maBang, string email, string vaiTro)
        {
            var userMoi = db.TaiKhoans.Where(u => u.DiaChiEmail == email).FirstOrDefault();

            // Nếu tìm thấy và chưa có trong bảng
            if (userMoi != null)
            {
                var check = db.ThanhVienBangs.Where(x => x.MaBang == maBang && x.MaTaiKhoan == userMoi.MaTaiKhoan).FirstOrDefault();
                if (check == null)
                {
                    ThanhVienBang tv = new ThanhVienBang();
                    tv.MaBang = maBang;
                    tv.MaTaiKhoan = userMoi.MaTaiKhoan;
                    tv.VaiTro = vaiTro;
                    tv.NgayThamGia = DateTime.Now;

                    db.ThanhVienBangs.Add(tv);
                    db.SaveChanges();
                }
            }
            // Load lại trang Share để thấy danh sách mới
            return RedirectToAction("Share", new { id = maBang });
        }

        // --- 11. XÓA THÀNH VIÊN ---
        [HttpPost] // Hoặc HttpGet
        public ActionResult RemoveMember(int maBang, int maTaiKhoan)
        {
            var tv = db.ThanhVienBangs.Where(x => x.MaBang == maBang && x.MaTaiKhoan == maTaiKhoan).FirstOrDefault();
            if (tv != null)
            {
                db.ThanhVienBangs.Remove(tv);
                db.SaveChanges();
            }
            return RedirectToAction("Share", new { id = maBang });
        }

        // --- 12. CẬP NHẬT QUYỀN ---
        [HttpPost]
        public ActionResult UpdateRole(int maBang, int maTaiKhoan, string vaiTro)
        {
            var tv = db.ThanhVienBangs.Where(x => x.MaBang == maBang && x.MaTaiKhoan == maTaiKhoan).FirstOrDefault();
            if (tv != null)
            {
                tv.VaiTro = vaiTro;
                db.SaveChanges();
            }
            return RedirectToAction("Share", new { id = maBang });
        }

        // --- 13. RỜI BẢNG ---
        [HttpPost]
        public ActionResult LeaveBoard(int maBang)
        {
            string email = User.Identity.Name;
            var user = db.TaiKhoans.Where(u => u.DiaChiEmail == email).FirstOrDefault();

            var tv = db.ThanhVienBangs.Where(x => x.MaBang == maBang && x.MaTaiKhoan == user.MaTaiKhoan).FirstOrDefault();
            if (tv != null)
            {
                db.ThanhVienBangs.Remove(tv);
                db.SaveChanges();
            }
            return RedirectToAction("Index", "Home");
        }
    }
}