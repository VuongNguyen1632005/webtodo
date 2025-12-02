-- =============================================================================
-- Script tạo Database cho dự án WebTodo (Trello Clone)
-- Database: QL_DUANCANHAN_LITE
-- =============================================================================

-- Tạo Database (nếu chưa tồn tại)
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'QL_DUANCANHAN_LITE')
BEGIN
    CREATE DATABASE QL_DUANCANHAN_LITE;
END
GO

USE QL_DUANCANHAN_LITE;
GO

-- =============================================================================
-- 1. Bảng TaiKhoan (Tài khoản người dùng)
-- =============================================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TaiKhoan')
BEGIN
    CREATE TABLE TaiKhoan (
        MaTaiKhoan INT IDENTITY(1,1) PRIMARY KEY,
        DiaChiEmail NVARCHAR(255) NOT NULL UNIQUE,
        MatKhau NVARCHAR(255) NOT NULL,
        HoTen NVARCHAR(100) NOT NULL,
        AnhDaiDien NVARCHAR(500) NULL
    );
    PRINT N'Đã tạo bảng TaiKhoan';
END
GO

-- =============================================================================
-- 2. Bảng Bang (Bảng công việc - Board)
-- =============================================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Bang')
BEGIN
    CREATE TABLE Bang (
        MaBang INT IDENTITY(1,1) PRIMARY KEY,
        MaNguoiSoHuu INT NOT NULL,
        TenBang NVARCHAR(200) NOT NULL,
        MauNen NVARCHAR(7) DEFAULT '#0079bf',
        NgayTao DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_Bang_TaiKhoan FOREIGN KEY (MaNguoiSoHuu) 
            REFERENCES TaiKhoan(MaTaiKhoan) ON DELETE CASCADE
    );
    PRINT N'Đã tạo bảng Bang';
END
GO

-- =============================================================================
-- 3. Bảng Cot (Cột trong bảng - Column/List)
-- =============================================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Cot')
BEGIN
    CREATE TABLE Cot (
        MaCot INT IDENTITY(1,1) PRIMARY KEY,
        MaBang INT NOT NULL,
        TenCot NVARCHAR(200) NOT NULL,
        ThuTu INT DEFAULT 0,
        KichHoat BIT DEFAULT 1,
        CONSTRAINT FK_Cot_Bang FOREIGN KEY (MaBang) 
            REFERENCES Bang(MaBang) ON DELETE CASCADE
    );
    PRINT N'Đã tạo bảng Cot';
END
GO

-- =============================================================================
-- 4. Bảng The (Thẻ công việc - Card)
-- =============================================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'The')
BEGIN
    CREATE TABLE The (
        MaThe INT IDENTITY(1,1) PRIMARY KEY,
        MaCot INT NOT NULL,
        TieuDe NVARCHAR(500) NOT NULL,
        MoTa NVARCHAR(MAX) NULL,
        HanChot DATETIME NULL,
        DaHoanThanh BIT DEFAULT 0,
        ThuTu INT DEFAULT 0,
        CONSTRAINT FK_The_Cot FOREIGN KEY (MaCot) 
            REFERENCES Cot(MaCot) ON DELETE CASCADE
    );
    PRINT N'Đã tạo bảng The';
END
GO

-- =============================================================================
-- 5. Bảng GhiChu (Ghi chú/Comment trong thẻ)
-- =============================================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GhiChu')
BEGIN
    CREATE TABLE GhiChu (
        MaGhiChu INT IDENTITY(1,1) PRIMARY KEY,
        MaThe INT NOT NULL,
        NoiDung NVARCHAR(MAX) NOT NULL,
        NgayTao DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_GhiChu_The FOREIGN KEY (MaThe) 
            REFERENCES The(MaThe) ON DELETE CASCADE
    );
    PRINT N'Đã tạo bảng GhiChu';
END
GO

-- =============================================================================
-- 6. Bảng MucTieu (Checklist item trong thẻ)
-- =============================================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MucTieu')
BEGIN
    CREATE TABLE MucTieu (
        MaMucTieu INT IDENTITY(1,1) PRIMARY KEY,
        MaThe INT NOT NULL,
        TenMucTieu NVARCHAR(500) NOT NULL,
        DaKiemTra BIT DEFAULT 0,
        ThuTu INT DEFAULT 0,
        CONSTRAINT FK_MucTieu_The FOREIGN KEY (MaThe) 
            REFERENCES The(MaThe) ON DELETE CASCADE
    );
    PRINT N'Đã tạo bảng MucTieu';
END
GO

-- =============================================================================
-- 7. Bảng NhanCuaBang (Nhãn của bảng - Labels)
-- =============================================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'NhanCuaBang')
BEGIN
    CREATE TABLE NhanCuaBang (
        MaNhanCuaBang INT IDENTITY(1,1) PRIMARY KEY,
        MaBang INT NOT NULL,
        TenHienThi NVARCHAR(100) NOT NULL,
        MaMau NVARCHAR(7) NOT NULL,
        CONSTRAINT FK_NhanCuaBang_Bang FOREIGN KEY (MaBang) 
            REFERENCES Bang(MaBang) ON DELETE CASCADE
    );
    PRINT N'Đã tạo bảng NhanCuaBang';
END
GO

-- =============================================================================
-- 8. Bảng NhanCuaThe (Gán nhãn cho thẻ - Junction table)
-- =============================================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'NhanCuaThe')
BEGIN
    CREATE TABLE NhanCuaThe (
        MaNhanCuaBang INT NOT NULL,
        MaThe INT NOT NULL,
        CONSTRAINT PK_NhanCuaThe PRIMARY KEY (MaNhanCuaBang, MaThe),
        CONSTRAINT FK_NhanCuaThe_NhanCuaBang FOREIGN KEY (MaNhanCuaBang) 
            REFERENCES NhanCuaBang(MaNhanCuaBang) ON DELETE NO ACTION,
        CONSTRAINT FK_NhanCuaThe_The FOREIGN KEY (MaThe) 
            REFERENCES The(MaThe) ON DELETE CASCADE
    );
    PRINT N'Đã tạo bảng NhanCuaThe';
END
GO

-- =============================================================================
-- 9. Bảng ThanhVienBang (Thành viên được chia sẻ bảng)
-- =============================================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ThanhVienBang')
BEGIN
    CREATE TABLE ThanhVienBang (
        MaBang INT NOT NULL,
        MaTaiKhoan INT NOT NULL,
        VaiTro NVARCHAR(20) DEFAULT 'viewer',  -- 'viewer', 'editor', 'owner'
        NgayThamGia DATETIME DEFAULT GETDATE(),
        CONSTRAINT PK_ThanhVienBang PRIMARY KEY (MaBang, MaTaiKhoan),
        CONSTRAINT FK_ThanhVienBang_Bang FOREIGN KEY (MaBang) 
            REFERENCES Bang(MaBang) ON DELETE CASCADE,
        CONSTRAINT FK_ThanhVienBang_TaiKhoan FOREIGN KEY (MaTaiKhoan) 
            REFERENCES TaiKhoan(MaTaiKhoan) ON DELETE NO ACTION
    );
    PRINT N'Đã tạo bảng ThanhVienBang';
END
GO

-- =============================================================================
-- TẠO DỮ LIỆU MẪU (SAMPLE DATA)
-- =============================================================================
PRINT N'';
PRINT N'=== Tạo dữ liệu mẫu ===';

-- Tạo tài khoản admin mẫu (password: 123)
IF NOT EXISTS (SELECT * FROM TaiKhoan WHERE DiaChiEmail = 'admin@test.com')
BEGIN
    INSERT INTO TaiKhoan (DiaChiEmail, MatKhau, HoTen) 
    VALUES ('admin@test.com', '123', N'Admin');
    PRINT N'Đã tạo tài khoản admin@test.com (mật khẩu: 123)';
END

-- Tạo tài khoản user mẫu để test chia sẻ
IF NOT EXISTS (SELECT * FROM TaiKhoan WHERE DiaChiEmail = 'user@test.com')
BEGIN
    INSERT INTO TaiKhoan (DiaChiEmail, MatKhau, HoTen) 
    VALUES ('user@test.com', '123', N'User Test');
    PRINT N'Đã tạo tài khoản user@test.com (mật khẩu: 123)';
END

-- Tạo bảng mẫu
DECLARE @AdminId INT = (SELECT MaTaiKhoan FROM TaiKhoan WHERE DiaChiEmail = 'admin@test.com');

IF NOT EXISTS (SELECT * FROM Bang WHERE TenBang = N'Dự án Trello Clone' AND MaNguoiSoHuu = @AdminId)
BEGIN
    INSERT INTO Bang (MaNguoiSoHuu, TenBang, MauNen)
    VALUES (@AdminId, N'Dự án Trello Clone', '#0079bf');
    
    DECLARE @BangId INT = SCOPE_IDENTITY();
    
    -- Tạo các cột mẫu
    INSERT INTO Cot (MaBang, TenCot, ThuTu) VALUES 
    (@BangId, N'Cần làm (To Do)', 0),
    (@BangId, N'Đang làm (Doing)', 1),
    (@BangId, N'Đã xong (Done)', 2);
    
    -- Tạo nhãn mẫu
    INSERT INTO NhanCuaBang (MaBang, TenHienThi, MaMau) VALUES 
    (@BangId, N'Ưu tiên cao', '#eb5a46'),
    (@BangId, N'Ưu tiên trung bình', '#f2d600'),
    (@BangId, N'Ưu tiên thấp', '#61bd4f');
    
    PRINT N'Đã tạo bảng mẫu với 3 cột và 3 nhãn';
END
GO

-- =============================================================================
-- HIỂN THỊ THÔNG TIN DATABASE
-- =============================================================================
PRINT N'';
PRINT N'=== Hoàn tất tạo Database ===';
PRINT N'Database: QL_DUANCANHAN_LITE';
PRINT N'';
PRINT N'Danh sách bảng đã tạo:';
PRINT N'1. TaiKhoan    - Quản lý tài khoản người dùng';
PRINT N'2. Bang        - Quản lý bảng công việc';
PRINT N'3. Cot         - Quản lý cột trong bảng';
PRINT N'4. The         - Quản lý thẻ công việc';
PRINT N'5. GhiChu      - Quản lý ghi chú trong thẻ';
PRINT N'6. MucTieu     - Quản lý checklist trong thẻ';
PRINT N'7. NhanCuaBang - Quản lý nhãn của bảng';
PRINT N'8. NhanCuaThe  - Gán nhãn cho thẻ';
PRINT N'9. ThanhVienBang - Chia sẻ bảng với người dùng khác';
PRINT N'';
PRINT N'Tài khoản mẫu:';
PRINT N'- admin@test.com / 123';
PRINT N'- user@test.com / 123';
GO
