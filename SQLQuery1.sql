CREATE DATABASE QL_DUANCANHAN_LITE
GO
USE QL_DUANCANHAN_LITE
GO

-- 1. TaiKhoan (Vẫn giữ để bạn đăng nhập)
CREATE TABLE TaiKhoan (
    MaTaiKhoan INT IDENTITY(1,1) PRIMARY KEY,
    DiaChiEmail NVARCHAR(100) NOT NULL UNIQUE,
    MatKhau NVARCHAR(255) NOT NULL, -- Nhớ Hash mật khẩu
    HoTen NVARCHAR(100) NULL,
    AnhDaiDien NVARCHAR(500) NULL
);
GO

-- 2. Bang (Bỏ cột CongKhai vì chỉ mình bạn xem)
CREATE TABLE Bang (
    MaBang INT IDENTITY(1,1) PRIMARY KEY,
    MaNguoiSoHuu INT NOT NULL, -- Link tới TaiKhoan
    TenBang NVARCHAR(100) NOT NULL,
    MauNen NVARCHAR(20) DEFAULT '#0079bf', -- Màu nền bảng
    NgayTao DATETIME DEFAULT GETDATE(),
    
    FOREIGN KEY (MaNguoiSoHuu) REFERENCES TaiKhoan(MaTaiKhoan)
);
GO

-- 3. Cot (Thêm KichHoat để Archive)
CREATE TABLE Cot (
    MaCot INT IDENTITY(1,1) PRIMARY KEY,
    MaBang INT NOT NULL,
    TenCot NVARCHAR(100) NOT NULL,
    ThuTu INT NOT NULL DEFAULT 0, -- Rất quan trọng để sắp xếp
    KichHoat BIT DEFAULT 1, 
    
    FOREIGN KEY (MaBang) REFERENCES Bang(MaBang) ON DELETE CASCADE
);
GO

-- 4. NhanCuaBang (Nhãn định nghĩa riêng cho từng bảng)
-- Logic mới: Mỗi bảng tự quản lý bộ nhãn của mình
CREATE TABLE NhanCuaBang (
    MaNhanCuaBang INT IDENTITY(1,1) PRIMARY KEY,
    MaBang INT NOT NULL,
    TenHienThi NVARCHAR(50) NULL, -- Ví dụ: "Gấp", "Đang chờ"
    MaMau NVARCHAR(7) NOT NULL,   -- Ví dụ: #ff0000
    
    FOREIGN KEY (MaBang) REFERENCES Bang(MaBang) ON DELETE CASCADE
);
GO

-- 5. The (Cốt lõi)
CREATE TABLE The (
    MaThe INT IDENTITY(1,1) PRIMARY KEY,
    MaCot INT NOT NULL,
    TieuDe NVARCHAR(255) NOT NULL,
    MoTa NVARCHAR(MAX) NULL,
    HanChot DATETIME NULL,       -- Deadline
    DaHoanThanh BIT DEFAULT 0,   -- Check nếu xong deadline
    ThuTu INT NOT NULL DEFAULT 0,-- Quan trọng để kéo thả
    
    FOREIGN KEY (MaCot) REFERENCES Cot(MaCot) ON DELETE CASCADE
);
GO

-- 1. Xóa bảng cũ nếu có
IF OBJECT_ID('dbo.NhanCuaThe', 'U') IS NOT NULL 
  DROP TABLE dbo.NhanCuaThe; 
GO

-- 2. Tạo bảng mới (Phiên bản KHÔNG lỗi)
CREATE TABLE NhanCuaThe (
    MaNhanCuaBang INT NOT NULL,
    MaThe INT NOT NULL,
    
    PRIMARY KEY (MaNhanCuaBang, MaThe),
    
    -- CHỈ GIỮ LẠI 1 KHÓA NGOẠI DUY NHẤT
    -- Nếu xóa Thẻ (The), dòng này tự mất. Đây là cái quan trọng nhất.
    FOREIGN KEY (MaThe) REFERENCES The(MaThe) ON DELETE CASCADE
    
    -- CHÚNG TA ĐÃ BỎ KHÓA NGOẠI TỚI 'NhanCuaBang'
    -- Database sẽ coi MaNhanCuaBang chỉ là một con số bình thường.
    -- Không còn lỗi Cycle, không cần code xử lý thêm.
);
GO

-- 7. ChiTiet (Gộp Comment và Activity log làm một cho đơn giản)
-- Dùng để bạn ghi chú tiến độ (VD: "Đã làm xong phần A, mai làm phần B")
CREATE TABLE GhiChu (
    MaGhiChu INT IDENTITY(1,1) PRIMARY KEY,
    MaThe INT NOT NULL,
    NoiDung NVARCHAR(1000) NOT NULL,
    NgayTao DATETIME DEFAULT GETDATE(),
    
    FOREIGN KEY (MaThe) REFERENCES The(MaThe) ON DELETE CASCADE
);
GO

-- 8. Checklist (MucTieu) - Giữ nguyên vì rất hữu ích cho cá nhân
CREATE TABLE MucTieu (
    MaMucTieu INT IDENTITY(1,1) PRIMARY KEY,
    MaThe INT NOT NULL,
    TenMucTieu NVARCHAR(255) NOT NULL,
    DaKiemTra BIT DEFAULT 0,
    ThuTu INT NOT NULL,
    
    FOREIGN KEY (MaThe) REFERENCES The(MaThe) ON DELETE CASCADE
);
GO