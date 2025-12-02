-- Script để thêm bảng ThanhVienBang và các cột mới
-- Chạy script này trên database QL_DUANCANHAN_LITE

USE QL_DUANCANHAN_LITE
GO

-- 1. Tạo bảng ThanhVienBang (nếu chưa tồn tại)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ThanhVienBang')
BEGIN
    CREATE TABLE ThanhVienBang (
        MaBang INT NOT NULL,
        MaTaiKhoan INT NOT NULL,
        VaiTro NVARCHAR(20) DEFAULT 'viewer',
        NgayThamGia DATETIME DEFAULT GETDATE(),
        CONSTRAINT PK_ThanhVienBang PRIMARY KEY (MaBang, MaTaiKhoan),
        CONSTRAINT FK_ThanhVienBang_Bang FOREIGN KEY (MaBang) REFERENCES Bang(MaBang) ON DELETE CASCADE,
        CONSTRAINT FK_ThanhVienBang_TaiKhoan FOREIGN KEY (MaTaiKhoan) REFERENCES TaiKhoan(MaTaiKhoan) ON DELETE CASCADE
    );
END
GO

-- 2. Thêm cột AnhDaiDien vào TaiKhoan (nếu chưa tồn tại)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TaiKhoan' AND COLUMN_NAME = 'AnhDaiDien')
BEGIN
    ALTER TABLE TaiKhoan ADD AnhDaiDien NVARCHAR(500) NULL;
END
GO

-- 3. Thêm cột MauNen cho bảng (nếu chưa tồn tại)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Bang' AND COLUMN_NAME = 'MauNen')
BEGIN
    ALTER TABLE Bang ADD MauNen NVARCHAR(7) DEFAULT '#0079bf';
END
GO

-- 4. Thêm cột KichHoat cho Cột (nếu chưa tồn tại)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Cot' AND COLUMN_NAME = 'KichHoat')
BEGIN
    ALTER TABLE Cot ADD KichHoat BIT DEFAULT 1;
END
GO

-- 5. Thêm cột ThuTu cho Thẻ (nếu chưa tồn tại)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'The' AND COLUMN_NAME = 'ThuTu')
BEGIN
    ALTER TABLE The ADD ThuTu INT DEFAULT 0;
END
GO

-- 6. Thêm cột DaHoanThanh cho Thẻ (nếu chưa tồn tại)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'The' AND COLUMN_NAME = 'DaHoanThanh')
BEGIN
    ALTER TABLE The ADD DaHoanThanh BIT DEFAULT 0;
END
GO
