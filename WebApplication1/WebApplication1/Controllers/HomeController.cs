using System;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;
using System.Collections.Generic; 

namespace WebApplication1.Controllers
{
    // Bắt buộc phải đăng nhập mới vào được Controller này
    [Authorize]
    public class HomeController : Controller
    {
        private QL_DUANCANHAN_LITEEntities db = new QL_DUANCANHAN_LITEEntities();

        public ActionResult Index()
        {
            // Lấy Email của người đang đăng nhập (từ Cookie)
            string emailDangNhap = User.Identity.Name;

            // Tìm ID của người đó trong Database
            var user = db.TaiKhoans.FirstOrDefault(u => u.DiaChiEmail == emailDangNhap);

            if (user != null)
            {
                // Lấy những Bảng thuộc về người này (chủ sở hữu)
                var bangCuaToi = db.Bangs
                                     .Where(b => b.MaNguoiSoHuu == user.MaTaiKhoan)
                                     .ToList();

                // Lấy những Bảng được chia sẻ với người này
                var bangDuocChiaSe = db.ThanhVienBangs
                                        .Where(tv => tv.MaTaiKhoan == user.MaTaiKhoan)
                                        .Select(tv => tv.Bang)
                                        .ToList();

                // Truyền riêng 2 danh sách xuống View
                ViewBag.BangCuaToi = bangCuaToi;
                ViewBag.BangDuocChiaSe = bangDuocChiaSe;

                // Trả về tất cả các bảng (gộp lại) cho View model
                var tatCaBang = bangCuaToi.Concat(bangDuocChiaSe).ToList();
                return View(tatCaBang);
            }

            return View(new List<Bang>());
        }
    }
}