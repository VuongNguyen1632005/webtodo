using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        private QL_DUANCANHAN_LITEEntities1 db = new QL_DUANCANHAN_LITEEntities1();

        // Trang Đăng Ký (GET)
        public ActionResult Register()
        {
            return View();
        }

        // Xử lý Đăng Ký (POST)
        [HttpPost]
        public ActionResult Register(string fullName, string email, string password, string confirmPassword)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    ViewBag.Error = "Vui lòng nhập họ tên!";
                    return View();
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    ViewBag.Error = "Vui lòng nhập email!";
                    return View();
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    ViewBag.Error = "Vui lòng nhập mật khẩu!";
                    return View();
                }

                if (password != confirmPassword)
                {
                    ViewBag.Error = "Mật khẩu xác nhận không khớp!";
                    return View();
                }

                if (password.Length < 3)
                {
                    ViewBag.Error = "Mật khẩu phải có ít nhất 3 ký tự!";
                    return View();
                }

                // Trim dữ liệu
                fullName = fullName.Trim();
                email = email.Trim();
                password = password.Trim();

                // Kiểm tra email đã tồn tại
                if (db.TaiKhoans.Any(x => x.DiaChiEmail == email))
                {
                    ViewBag.Error = "Email này đã được sử dụng!";
                    return View();
                }

                // Tạo tài khoản mới
                var newUser = new TaiKhoan
                {
                    HoTen = fullName,
                    DiaChiEmail = email,
                    MatKhau = password
                };

                db.TaiKhoans.Add(newUser);
                db.SaveChanges();

                // Chuyển đến trang login
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi hệ thống: " + ex.Message;
                if (ex.InnerException != null)
                {
                    ViewBag.Error += " | Chi tiết: " + ex.InnerException.Message;
                }
                return View();
            }
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
            try
            {
                // Loại bỏ khoảng trắng thừa
                if (!string.IsNullOrEmpty(email))
                    email = email.Trim();
                if (!string.IsNullOrEmpty(password))
                    password = password.Trim();

                // Tìm user trong DB
                var user = db.TaiKhoans.FirstOrDefault(u => u.DiaChiEmail == email && u.MatKhau == password);

                if (user != null)
                {
                

                 
                    FormsAuthentication.SetAuthCookie(user.DiaChiEmail, false);

                  
                    Session["HoTen"] = user.HoTen;

                 
                    Session["MaTaiKhoan"] = user.MaTaiKhoan;

                    return RedirectToAction("Index", "Home");
                }

              
                ViewBag.Error = "Sai email hoặc mật khẩu!";
                return View();
            }
            catch (System.Data.Entity.Core.EntityException ex)
            {
              
                ViewBag.Error = "❌ Không thể kết nối tới SQL Server!";
                ViewBag.ErrorDetail = ex.InnerException?.Message ?? ex.Message;
                ViewBag.Solution = @" ";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi không xác định: " + ex.Message;
                if (ex.InnerException != null)
                {
                    ViewBag.ErrorDetail = ex.InnerException.Message;
                }
                return View();
            }
        }

        // Đăng Xuất
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Login");
        }
    }
}