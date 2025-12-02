USE QL_DUANCANHAN_LITE
GO

-- 1. Tạo user admin (Pass là 123)
INSERT INTO TaiKhoan (DiaChiEmail, MatKhau, HoTen) 
VALUES ('admin@test.com', '123', N'Admin Đẹp Trai');

-- 2. Lấy ID vừa tạo (thường là 1) để tạo Bảng
INSERT INTO Bang (MaNguoiSoHuu, TenBang, MauNen)
VALUES (1, N'Dự án Trello Clone', '#0079bf');

-- 3. Tạo cột mẫu
INSERT INTO Cot (MaBang, TenCot, ThuTu) VALUES 
(1, N'Cần làm (To Do)', 0),
(1, N'Đang làm (Doing)', 1),
(1, N'Đã xong (Done)', 2);