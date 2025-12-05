using System;
using System.Data.Entity;
using System.Linq;
using WebApplication1.Models;

namespace WebApplication1
{
    // Database Initializer ?? t? ??ng t?o DB và seed data
    public class DatabaseInitializer : CreateDatabaseIfNotExists<QL_DUANCANHAN_LITEEntities>
    {
        protected override void Seed(QL_DUANCANHAN_LITEEntities context)
        {
            // Ki?m tra xem ?ã có user ch?a
            if (!context.TaiKhoans.Any())
            {
                // T?o tài kho?n admin
                var admin = new TaiKhoan
                {
                    DiaChiEmail = "admin@test.com",
                    MatKhau = "123",
                    HoTen = "Admin ??p Trai"
                };
                context.TaiKhoans.Add(admin);
                context.SaveChanges();

                // T?o tài kho?n user m?u
                var user = new TaiKhoan
                {
                    DiaChiEmail = "user@test.com",
                    MatKhau = "123",
                    HoTen = "User Test"
                };
                context.TaiKhoans.Add(user);
                context.SaveChanges();

                // T?o b?ng m?u
                var bang = new Bang
                {
                    MaNguoiSoHuu = admin.MaTaiKhoan,
                    TenBang = "D? án Trello Clone",
                    MauNen = "#0079bf",
                    NgayTao = DateTime.Now
                };
                context.Bangs.Add(bang);
                context.SaveChanges();

                // T?o c?t m?u
                var cot1 = new Cot { MaBang = bang.MaBang, TenCot = "C?n làm (To Do)", ThuTu = 0, KichHoat = true };
                var cot2 = new Cot { MaBang = bang.MaBang, TenCot = "?ang làm (Doing)", ThuTu = 1, KichHoat = true };
                var cot3 = new Cot { MaBang = bang.MaBang, TenCot = "?ã xong (Done)", ThuTu = 2, KichHoat = true };
                
                context.Cots.Add(cot1);
                context.Cots.Add(cot2);
                context.Cots.Add(cot3);
                context.SaveChanges();

                // T?o nhãn m?u
                var nhan1 = new NhanCuaBang { MaBang = bang.MaBang, TenHienThi = "?u tiên cao", MaMau = "#eb5a46" };
                var nhan2 = new NhanCuaBang { MaBang = bang.MaBang, TenHienThi = "?u tiên trung bình", MaMau = "#f2d600" };
                var nhan3 = new NhanCuaBang { MaBang = bang.MaBang, TenHienThi = "?u tiên th?p", MaMau = "#61bd4f" };
                
                context.NhanCuaBangs.Add(nhan1);
                context.NhanCuaBangs.Add(nhan2);
                context.NhanCuaBangs.Add(nhan3);
                context.SaveChanges();
            }

            base.Seed(context);
        }
    }
}
