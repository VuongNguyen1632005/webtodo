using System;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;
using System.Collections.Generic; // Để dùng List

namespace WebApplication1.Controllers
{
    // 👇 Dòng này cực quan trọng: Bắt buộc phải đăng nhập mới vào được Controller này
    [Authorize]
    public class HomeController : Controller
    {
        private QL_DUANCANHAN_LITEEntities db = new QL_DUANCANHAN_LITEEntities();

        public ActionResult Index()
        {
            // 1. Lấy Email của người đang đăng nhập (từ Cookie)
            string emailDangNhap = User.Identity.Name;

            // 2. Tìm ID của người đó trong Database
            var user = db.TaiKhoans.FirstOrDefault(u => u.DiaChiEmail == emailDangNhap);

            if (user != null)
            {
                // 3. Chỉ lấy những Bảng thuộc về người này
                var danhSachBang = db.Bangs
                                     .Where(b => b.MaNguoiSoHuu == user.MaTaiKhoan)
                                     .ToList();

                return View(danhSachBang);
            }

            return View(new List<Bang>());
        }
    }
}