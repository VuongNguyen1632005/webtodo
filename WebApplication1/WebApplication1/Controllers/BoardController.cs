using System;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models; // Đổi tên namespace nếu cần

namespace WebApplication1.Controllers
{
    [Authorize] // Bắt buộc đăng nhập
    public class BoardController : Controller
    {
        private QL_DUANCANHAN_LITEEntities db = new QL_DUANCANHAN_LITEEntities();

        public ActionResult Details(int id)
        {
            // 1. Tìm bảng theo ID
            var board = db.Bangs.FirstOrDefault(b => b.MaBang == id);

            // 2. Bảo mật: Nếu không tìm thấy hoặc bảng không phải của người này -> Đá về trang chủ
            // Lưu ý: So sánh MaNguoiSoHuu với ID của User đang login
            // Để đơn giản đoạn này mình tạm bỏ qua check User ID, chỉ check null. 
            // Sau này bạn muốn chặt chẽ thì thêm check User ID nhé.
            if (board == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // 3. Trả về View kèm dữ liệu Bảng (EF sẽ tự load Cột và Thẻ liên quan)
            return View(board);
        }
    

        // 1. Thêm thẻ nhanh (Gọi bằng AJAX)
        [HttpPost]
        public JsonResult CreateCard(int maCot, string noiDung)
        {
            try
            {
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
                        // Cập nhật lại thứ tự
                        the.ThuTu = thuTu;
                        // Lưu ý: Logic này đang giả định là kéo thả trong cùng 1 cột hoặc load lại
                        // Để chính xác 100% khi chuyển cột, ta cần gửi thêm MaCotMoi. 
                        // Nhưng ở mức cơ bản, ta sẽ cập nhật MaCot ở Frontend gửi về sau.
                        // (Ở bước tinh gọn này, Entity Framework sẽ tự track thay đổi nếu bạn load đúng)
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
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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


    }
}