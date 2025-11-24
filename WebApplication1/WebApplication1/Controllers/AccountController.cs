using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security; // Thư viện bảo mật login
using WebApplication1.Models; // Đổi thành tên Project của bạn

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        private QL_DUANCANHAN_LITEEntities db = new QL_DUANCANHAN_LITEEntities();

        // 1. Trang Đăng Ký (GET)
        public ActionResult Register()
        {
            return View();
        }

        // 2. Xử lý Đăng Ký (POST)
        [HttpPost]
        public ActionResult Register(TaiKhoan model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra email trùng
                if (db.TaiKhoans.Any(x => x.DiaChiEmail == model.DiaChiEmail))
                {
                    ModelState.AddModelError("", "Email này đã tồn tại.");
                    return View(model);
                }

                // Lưu vào DB (Lưu ý: Tạm thời lưu pass thường, sau này thêm mã hóa sau cho nhanh)
                db.TaiKhoans.Add(model);
                db.SaveChanges();

                return RedirectToAction("Login");
            }
            return View(model);
        }

        // 3. Trang Đăng Nhập (GET)
        public ActionResult Login()
        {
            return View();
        }

        // 4. Xử lý Đăng Nhập (POST)
        [HttpPost]
        public ActionResult Login(string email, string password)
        {
            // Tìm user trong DB
            var user = db.TaiKhoans.FirstOrDefault(u => u.DiaChiEmail == email && u.MatKhau == password);

            if (user != null)
            {
                // Login thành công!
                // Lưu cookie phiên đăng nhập (Lưu Email vào cookie)
                FormsAuthentication.SetAuthCookie(user.DiaChiEmail, false);

                // Chuyển hướng về trang chủ
                return RedirectToAction("Index", "Home");
            }

            // Login thất bại
            ViewBag.Error = "Sai email hoặc mật khẩu!";
            return View();
        }

        // 5. Đăng Xuất
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }
    }
}