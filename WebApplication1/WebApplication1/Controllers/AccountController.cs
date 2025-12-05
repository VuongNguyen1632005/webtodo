using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security; // Thư viện bảo mật login
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        private QL_DUANCANHAN_LITEEntities db = new QL_DUANCANHAN_LITEEntities();

        // Trang Đăng Ký (GET)
        public ActionResult Register()
        {
            return View();
        }

        // Xử lý Đăng Ký (POST)
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

                // Lưu vào DB 
                db.TaiKhoans.Add(model);
                db.SaveChanges();

                return RedirectToAction("Login");
            }
            return View(model);
        }

        // Trang Đăng Nhập (GET)
        public ActionResult Login()
        {
            return View();
        }

        //Xử lý Đăng Nhập (POST)
        [HttpPost]
        public ActionResult Login(string email, string password)
        {
            // Tìm user trong DB
            var user = db.TaiKhoans.FirstOrDefault(u => u.DiaChiEmail == email && u.MatKhau == password);

            if (user != null)
            {
                // Login thành công!

                // Lưu Email để hệ thống biết đã đăng nhập
                FormsAuthentication.SetAuthCookie(user.DiaChiEmail, false);

                // Lưu Họ Tên vào Session để dùng ở _Layout
                Session["HoTen"] = user.HoTen;

                // Lưu thêm ID người dùng nếu cần dùng nhiều
                Session["MaTaiKhoan"] = user.MaTaiKhoan;

                return RedirectToAction("Index", "Home");
            }

            // Login thất bại
            ViewBag.Error = "Sai email hoặc mật khẩu!";
            return View();
        }

        // Đăng Xuất
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }
    }
}