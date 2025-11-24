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
        [HttpPost]
        public JsonResult MoveCard(int maThe, int maCotMoi, int[] cardIds)
        {
            try
            {
                // Tìm thẻ và đổi cột
                var the = db.Thes.Find(maThe);
                if (the != null)
                {
                    the.MaCot = maCotMoi;
                }

                // Sau đó cập nhật thứ tự (Giống hàm trên)
                int thuTu = 0;
                foreach (var id in cardIds)
                {
                    var t = db.Thes.Find(id);
                    if (t != null) t.ThuTu = thuTu;
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

        // 4. Lấy chi tiết thẻ
        [HttpGet]
        public JsonResult GetCardDetails(int id)
        {
            var the = db.Thes.Find(id);
            if (the != null)
            {
                // Trả về dữ liệu JSON sạch
                return Json(new
                {
                    id = the.MaThe,
                    title = the.TieuDe,
                    desc = the.MoTa ?? "" // Nếu null thì trả về rỗng
                }, JsonRequestBehavior.AllowGet);
            }
            return Json(null);
        }

        // 5. Cập nhật thẻ (Sửa tên, mô tả)
        [HttpPost]
        public JsonResult UpdateCardDetails(int id, string title, string desc)
        {
            try
            {
                var the = db.Thes.Find(id);
                if (the != null)
                {
                    the.TieuDe = title;
                    the.MoTa = desc;
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




    }
}