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
        private QL_DUANCANHAN_LITEEntities1 db = new QL_DUANCANHAN_LITEEntities1();

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

        // 8a. Xóa cột cùng các thẻ bên trong
        [HttpPost]
        public JsonResult DeleteColumn(int maCot)
        {
            try
            {
                var cot = db.Cots.Find(maCot);
                if (cot == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy cột" });
                }

                int userId = GetCurrentUserId();
                if (!CanEdit(cot.MaBang, userId))
                {
                    return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này" });
                }

                // Xóa các thẻ thuộc cột
                var cards = db.Thes.Where(t => t.MaCot == maCot).ToList();
                foreach (var card in cards)
                {
                    // Xóa ghi chú của thẻ
                    var notes = db.GhiChus.Where(g => g.MaThe == card.MaThe).ToList();
                    db.GhiChus.RemoveRange(notes);
                    
                    // Xóa thẻ
                    db.Thes.Remove(card);
                }

                // Xóa cột
                db.Cots.Remove(cot);
                db.SaveChanges();
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể xóa cột: " + ex.Message });
            }
        }

        // 8b. Đổi tên bảng
        [HttpPost]
        public JsonResult UpdateBoardName(int maBang, string tenMoi)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tenMoi))
                {
                    return Json(new { success = false, message = "Tên bảng không được rỗng" });
                }

                var bang = db.Bangs.Find(maBang);
                if (bang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bảng" });
                }

                int userId = GetCurrentUserId();
                if (!CanEdit(maBang, userId))
                {
                    return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này" });
                }

                bang.TenBang = tenMoi.Trim();
                db.SaveChanges();

                return Json(new { success = true, tenMoi = bang.TenBang });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể đổi tên bảng: " + ex.Message });
            }
        }

        // 8c. Tìm kiếm bảng
        [HttpGet]
        public JsonResult SearchBoards(string keyword)
        {
            try
            {
                int userId = GetCurrentUserId();
                if (userId == -1)
                {
                    return Json(new { success = false, message = "Chưa đăng nhập" }, JsonRequestBehavior.AllowGet);
                }

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return Json(new { success = true, boards = new List<object>() }, JsonRequestBehavior.AllowGet);
                }

                keyword = keyword.Trim().ToLower();

                // Tìm bảng của tôi
                var myBoards = db.Bangs
                    .Where(b => b.MaNguoiSoHuu == userId && b.TenBang.ToLower().Contains(keyword))
                    .Select(b => new
                    {
                        maBang = b.MaBang,
                        tenBang = b.TenBang,
                        mauNen = b.MauNen ?? "#0079bf",
                        isOwner = true
                    })
                    .Take(5)
                    .ToList();

                // Tìm bảng được chia sẻ
                var sharedBoards = db.ThanhVienBangs
                    .Where(tv => tv.MaTaiKhoan == userId && tv.Bang.TenBang.ToLower().Contains(keyword))
                    .Select(tv => new
                    {
                        maBang = tv.MaBang,
                        tenBang = tv.Bang.TenBang,
                        mauNen = tv.Bang.MauNen ?? "#0079bf",
                        isOwner = false
                    })
                    .Take(5)
                    .ToList();

                var allBoards = myBoards.Concat(sharedBoards).ToList();

                return Json(new { success = true, boards = allBoards }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // 8d. Xóa bảng
        [HttpPost]
        public JsonResult DeleteBoard(int maBang)
        {
            try
            {
                var bang = db.Bangs.Find(maBang);
                if (bang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bảng" });
                }

                int userId = GetCurrentUserId();
                if (!CanManage(maBang, userId))
                {
                    return Json(new { success = false, message = "Chỉ chủ sở hữu mới có thể xóa bảng" });
                }

                // Xóa tất cả thành viên
                var members = db.ThanhVienBangs.Where(tv => tv.MaBang == maBang).ToList();
                db.ThanhVienBangs.RemoveRange(members);

                // Xóa tất cả cột và thẻ
                var columns = db.Cots.Where(c => c.MaBang == maBang).ToList();
                foreach (var col in columns)
                {
                    var cards = db.Thes.Where(t => t.MaCot == col.MaCot).ToList();
                    foreach (var card in cards)
                    {
                        // Xóa comments của thẻ
                        var comments = db.GhiChus.Where(g => g.MaThe == card.MaThe).ToList();
                        db.GhiChus.RemoveRange(comments);
                        
                        db.Thes.Remove(card);
                    }
                    db.Cots.Remove(col);
                }

                // Xóa bảng
                db.Bangs.Remove(bang);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Không thể xóa bảng: " + ex.Message });
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

        // 10. Lấy danh sách comment của thẻ
        [HttpGet]
        public JsonResult GetCardComments(int maThe)
        {
            try
            {
                int userId = GetCurrentUserId();
                var currentUser = db.TaiKhoans.FirstOrDefault(u => u.MaTaiKhoan == userId);

                // Load comments vào memory để tránh lỗi provider khi join navigation
                var commentsRaw = db.GhiChus
                    .Where(g => g.MaThe == maThe)
                    .OrderByDescending(g => g.NgayTao)
                    .ToList();

                // Lấy danh sách user liên quan
                var userIds = commentsRaw.Where(c => c.MaTaiKhoan.HasValue)
                                          .Select(c => c.MaTaiKhoan.Value)
                                          .Distinct()
                                          .ToList();
                var users = db.TaiKhoans
                              .Where(t => userIds.Contains(t.MaTaiKhoan))
                              .ToDictionary(t => t.MaTaiKhoan);

                var comments = commentsRaw.Select(c =>
                {
                    string name = "Người dùng";
                    if (c.MaTaiKhoan.HasValue && users.TryGetValue(c.MaTaiKhoan.Value, out var tk))
                    {
                        name = !string.IsNullOrWhiteSpace(tk.HoTen)
                            ? tk.HoTen
                            : (tk.DiaChiEmail?.Split('@').FirstOrDefault() ?? "Người dùng");
                    }
                    else if (currentUser != null)
                    {
                        name = !string.IsNullOrWhiteSpace(currentUser.HoTen)
                            ? currentUser.HoTen
                            : (currentUser.DiaChiEmail?.Split('@').FirstOrDefault() ?? "Bạn");
                    }
                    return new
                    {
                        id = c.MaGhiChu,
                        content = c.NoiDung,
                        createdAt = c.NgayTao,
                        userName = name
                    };
                }).ToList();

                return Json(new
                {
                    success = true,
                    comments,
                    currentUserName = currentUser != null
                        ? (!string.IsNullOrWhiteSpace(currentUser.HoTen) ? currentUser.HoTen : (currentUser.DiaChiEmail?.Split('@').FirstOrDefault() ?? "Bạn"))
                        : "Bạn"
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // 11. Thêm comment mới
        [HttpPost]
        public JsonResult AddCardComment(int maThe, string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { success = false, message = "Nội dung không được trống" });
                }

                var the = db.Thes.Find(maThe);
                if (the == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thẻ" });
                }

                // Kiểm tra quyền xem
                int userId = GetCurrentUserId();
                if (!CanView(the.Cot.MaBang, userId))
                {
                    return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này" });
                }

                var currentUser = db.TaiKhoans.Find(userId);

                var comment = new GhiChu
                {
                    MaThe = maThe,
                    MaTaiKhoan = userId,
                    NoiDung = content.Trim(),
                    NgayTao = DateTime.Now
                };

                db.GhiChus.Add(comment);
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    comment = new
                    {
                        id = comment.MaGhiChu,
                        content = comment.NoiDung,
                        createdAt = comment.NgayTao,
                        userName = currentUser?.HoTen ?? "Bạn",
                        userAvatar = currentUser?.AnhDaiDien
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 12. Xóa comment
        [HttpPost]
        public JsonResult DeleteCardComment(int id)
        {
            try
            {
                var comment = db.GhiChus.Find(id);
                if (comment != null)
                {
                    // Kiểm tra quyền sửa
                    int userId = GetCurrentUserId();
                    var the = db.Thes.Find(comment.MaThe);
                    if (the != null && CanEdit(the.Cot.MaBang, userId))
                    {
                        db.GhiChus.Remove(comment);
                        db.SaveChanges();
                        return Json(new { success = true });
                    }
                    return Json(new { success = false, message = "Bạn không có quyền xóa comment này" });
                }
            }
            catch { }
            return Json(new { success = false });
        }

        #region API Quản lý Nhãn (Labels)

        // 13. Lấy danh sách nhãn của bảng
        [HttpGet]
        public JsonResult GetBoardLabels(int maBang)
        {
            try
            {
                var labels = db.NhanCuaBangs
                    .Where(n => n.MaBang == maBang)
                    .Select(n => new
                    {
                        maNhan = n.MaNhanCuaBang,
                        tenHienThi = n.TenHienThi,
                        maMau = n.MaMau
                    })
                    .ToList();

                return Json(new { success = true, labels = labels }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // 14. Tạo nhãn mới cho bảng
        [HttpPost]
        public JsonResult CreateLabel(int maBang, string tenHienThi, string maMau)
        {
            try
            {
                int userId = GetCurrentUserId();
                if (!CanEdit(maBang, userId))
                {
                    return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này" });
                }

                var nhan = new NhanCuaBang
                {
                    MaBang = maBang,
                    TenHienThi = tenHienThi,
                    MaMau = maMau
                };

                db.NhanCuaBangs.Add(nhan);
                db.SaveChanges();

                return Json(new { success = true, maNhan = nhan.MaNhanCuaBang });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 15. Xóa nhãn
        [HttpPost]
        public JsonResult DeleteLabel(int maNhan)
        {
            try
            {
                var nhan = db.NhanCuaBangs.Find(maNhan);
                if (nhan == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy nhãn" });
                }

                int userId = GetCurrentUserId();
                if (!CanEdit(nhan.MaBang, userId))
                {
                    return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này" });
                }

                // Xóa các liên kết nhãn-thẻ trước
                var nhanCuaThes = db.NhanCuaThes.Where(n => n.MaNhanCuaBang == maNhan).ToList();
                db.NhanCuaThes.RemoveRange(nhanCuaThes);

                // Xóa nhãn
                db.NhanCuaBangs.Remove(nhan);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // 16. Lấy danh sách nhãn của thẻ
        [HttpGet]
        public JsonResult GetCardLabels(int maThe)
        {
            try
            {
                var the = db.Thes.Find(maThe);
                if (the == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thẻ" }, JsonRequestBehavior.AllowGet);
                }

                var labels = db.NhanCuaThes
                    .Where(n => n.MaThe == maThe)
                    .Select(n => new
                    {
                        maNhan = n.MaNhanCuaBang,
                        tenHienThi = n.NhanCuaBang.TenHienThi,
                        maMau = n.NhanCuaBang.MaMau
                    })
                    .ToList();

                return Json(new { success = true, labels = labels }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // 17. Gán/Bỏ nhãn cho thẻ (Toggle)
        [HttpPost]
        public JsonResult ToggleCardLabel(int maThe, int maNhan)
        {
            try
            {
                var the = db.Thes.Find(maThe);
                if (the == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thẻ" });
                }

                int userId = GetCurrentUserId();
                if (!CanEdit(the.Cot.MaBang, userId))
                {
                    return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này" });
                }

                var existing = db.NhanCuaThes.FirstOrDefault(n => n.MaThe == maThe && n.MaNhanCuaBang == maNhan);

                if (existing != null)
                {
                    // Đã có -> Bỏ nhãn
                    db.NhanCuaThes.Remove(existing);
                    db.SaveChanges();
                    return Json(new { success = true, action = "removed" });
                }
                else
                {
                    // Chưa có -> Gán nhãn
                    var nhanCuaThe = new NhanCuaThe
                    {
                        MaThe = maThe,
                        MaNhanCuaBang = maNhan
                    };
                    db.NhanCuaThes.Add(nhanCuaThe);
                    db.SaveChanges();
                    return Json(new { success = true, action = "added" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region API Quản lý Thành viên của Thẻ (Card Members)

        // 18. Lấy danh sách thành viên có thể gán cho thẻ (thành viên của bảng)
        [HttpGet]
        public JsonResult GetAvailableMembers(int maBang)
        {
            try
            {
                var bang = db.Bangs.Find(maBang);
                if (bang == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bảng" }, JsonRequestBehavior.AllowGet);
                }

                var members = new List<object>();

                // Thêm chủ sở hữu
                members.Add(new
                {
                    maTaiKhoan = bang.TaiKhoan.MaTaiKhoan,
                    hoTen = bang.TaiKhoan.HoTen ?? bang.TaiKhoan.DiaChiEmail.Split('@')[0],
                    email = bang.TaiKhoan.DiaChiEmail,
                    anhDaiDien = bang.TaiKhoan.AnhDaiDien
                });

                // Thêm các thành viên khác
                var thanhViens = db.ThanhVienBangs
                    .Where(tv => tv.MaBang == maBang)
                    .Select(tv => new
                    {
                        maTaiKhoan = tv.MaTaiKhoan,
                        hoTen = tv.TaiKhoan.HoTen ?? "",
                        email = tv.TaiKhoan.DiaChiEmail,
                        anhDaiDien = tv.TaiKhoan.AnhDaiDien
                    })
                    .ToList()
                    .Select(tv => new
                    {
                        maTaiKhoan = tv.maTaiKhoan,
                        hoTen = string.IsNullOrEmpty(tv.hoTen) ? tv.email.Split('@')[0] : tv.hoTen,
                        email = tv.email,
                        anhDaiDien = tv.anhDaiDien
                    })
                    .ToList();

                members.AddRange(thanhViens);

                return Json(new { success = true, members = members }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // 19. Lấy danh sách thành viên đã gán cho thẻ
        [HttpGet]
        public JsonResult GetCardMembers(int maThe)
        {
            try
            {
                var the = db.Thes.Find(maThe);
                if (the == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thẻ" }, JsonRequestBehavior.AllowGet);
                }

                var members = db.ThanhVienCuaThes
                    .Where(tv => tv.MaThe == maThe)
                    .Select(tv => new
                    {
                        maTaiKhoan = tv.MaTaiKhoan,
                        hoTen = tv.TaiKhoan.HoTen ?? "",
                        email = tv.TaiKhoan.DiaChiEmail,
                        anhDaiDien = tv.TaiKhoan.AnhDaiDien,
                        ngayGan = tv.NgayGan
                    })
                    .ToList()
                    .Select(tv => new
                    {
                        maTaiKhoan = tv.maTaiKhoan,
                        hoTen = string.IsNullOrEmpty(tv.hoTen) ? tv.email.Split('@')[0] : tv.hoTen,
                        email = tv.email,
                        anhDaiDien = tv.anhDaiDien,
                        ngayGan = tv.ngayGan
                    })
                    .ToList();

                return Json(new { success = true, members = members }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // 20. Gán/Bỏ thành viên cho thẻ (Toggle)
        [HttpPost]
        public JsonResult ToggleCardMember(int maThe, int maTaiKhoan)
        {
            try
            {
                var the = db.Thes.Find(maThe);
                if (the == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thẻ" });
                }

                int userId = GetCurrentUserId();
                if (!CanEdit(the.Cot.MaBang, userId))
                {
                    return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này" });
                }

                var existing = db.ThanhVienCuaThes.FirstOrDefault(tv => tv.MaThe == maThe && tv.MaTaiKhoan == maTaiKhoan);

                if (existing != null)
                {
                    // Đã có -> Bỏ thành viên
                    db.ThanhVienCuaThes.Remove(existing);
                    db.SaveChanges();
                    return Json(new { success = true, action = "removed" });
                }
                else
                {
                    // Chưa có -> Gán thành viên
                    var thanhVienCuaThe = new ThanhVienCuaThe
                    {
                        MaThe = maThe,
                        MaTaiKhoan = maTaiKhoan,
                        NgayGan = DateTime.Now
                    };
                    db.ThanhVienCuaThes.Add(thanhVienCuaThe);
                    db.SaveChanges();
                    return Json(new { success = true, action = "added" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion
    }
}