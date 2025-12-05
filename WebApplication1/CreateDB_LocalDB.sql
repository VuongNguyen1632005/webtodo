-- Script t?o Database cho LocalDB
-- Ch?y l?nh: sqlcmd -S (localdb)\MSSQLLocalDB -i CreateDB_LocalDB.sql

-- T?o Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'QL_DUANCANHAN_LITE')
BEGIN
    CREATE DATABASE QL_DUANCANHAN_LITE;
    PRINT N'? ?ã t?o database QL_DUANCANHAN_LITE';
END
ELSE
BEGIN
    PRINT N'Database QL_DUANCANHAN_LITE ?ã t?n t?i';
END
GO

USE QL_DUANCANHAN_LITE;
GO

-- T?o b?ng TaiKhoan
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TaiKhoan')
BEGIN
    CREATE TABLE TaiKhoan (
        MaTaiKhoan INT IDENTITY(1,1) PRIMARY KEY,
        DiaChiEmail NVARCHAR(255) NOT NULL UNIQUE,
        MatKhau NVARCHAR(255) NOT NULL,
        HoTen NVARCHAR(100) NOT NULL,
        AnhDaiDien NVARCHAR(500) NULL
    );
    PRINT N'? ?ã t?o b?ng TaiKhoan';
END
GO

-- T?o b?ng Bang
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
    PRINT N'? ?ã t?o b?ng Bang';
END
GO

-- T?o b?ng Cot
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
    PRINT N'? ?ã t?o b?ng Cot';
END
GO

-- T?o b?ng The
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
    PRINT N'? ?ã t?o b?ng The';
END
GO

-- T?o b?ng GhiChu
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
    PRINT N'? ?ã t?o b?ng GhiChu';
END
GO

-- T?o b?ng MucTieu
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
    PRINT N'? ?ã t?o b?ng MucTieu';
END
GO

-- T?o b?ng NhanCuaBang
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
    PRINT N'? ?ã t?o b?ng NhanCuaBang';
END
GO

-- T?o b?ng NhanCuaThe
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
    PRINT N'? ?ã t?o b?ng NhanCuaThe';
END
GO

-- T?o b?ng ThanhVienBang
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ThanhVienBang')
BEGIN
    CREATE TABLE ThanhVienBang (
        MaBang INT NOT NULL,
        MaTaiKhoan INT NOT NULL,
        VaiTro NVARCHAR(20) DEFAULT 'viewer',
        NgayThamGia DATETIME DEFAULT GETDATE(),
        CONSTRAINT PK_ThanhVienBang PRIMARY KEY (MaBang, MaTaiKhoan),
        CONSTRAINT FK_ThanhVienBang_Bang FOREIGN KEY (MaBang) 
            REFERENCES Bang(MaBang) ON DELETE CASCADE,
        CONSTRAINT FK_ThanhVienBang_TaiKhoan FOREIGN KEY (MaTaiKhoan) 
            REFERENCES TaiKhoan(MaTaiKhoan) ON DELETE NO ACTION
    );
    PRINT N'? ?ã t?o b?ng ThanhVienBang';
END
GO

-- Thêm d? li?u m?u
PRINT N'=== T?o d? li?u m?u ===';

-- Xóa d? li?u c? n?u có
DELETE FROM TaiKhoan WHERE DiaChiEmail IN ('admin@test.com', 'user@test.com');

-- T?o tài kho?n admin
INSERT INTO TaiKhoan (DiaChiEmail, MatKhau, HoTen) 
VALUES ('admin@test.com', '123', N'Admin ??p Trai');
PRINT N'? ?ã t?o user: admin@test.com / 123';

-- T?o tài kho?n user m?u
INSERT INTO TaiKhoan (DiaChiEmail, MatKhau, HoTen) 
VALUES ('user@test.com', '123', N'User Test');
PRINT N'? ?ã t?o user: user@test.com / 123';

-- L?y ID admin
DECLARE @AdminId INT = (SELECT MaTaiKhoan FROM TaiKhoan WHERE DiaChiEmail = 'admin@test.com');

-- T?o b?ng m?u
INSERT INTO Bang (MaNguoiSoHuu, TenBang, MauNen)
VALUES (@AdminId, N'D? án Trello Clone', '#0079bf');
PRINT N'? ?ã t?o b?ng m?u';

DECLARE @BangId INT = SCOPE_IDENTITY();

-- T?o c?t m?u
INSERT INTO Cot (MaBang, TenCot, ThuTu) VALUES 
(@BangId, N'C?n làm (To Do)', 0),
(@BangId, N'?ang làm (Doing)', 1),
(@BangId, N'?ã xong (Done)', 2);
PRINT N'? ?ã t?o 3 c?t m?u';

-- T?o nhãn m?u
INSERT INTO NhanCuaBang (MaBang, TenHienThi, MaMau) VALUES 
(@BangId, N'?u tiên cao', '#eb5a46'),
(@BangId, N'?u tiên trung bình', '#f2d600'),
(@BangId, N'?u tiên th?p', '#61bd4f');
PRINT N'? ?ã t?o 3 nhãn m?u';

GO

PRINT N'';
PRINT N'========================================';
PRINT N'? HOÀN T?T KH?I T?O DATABASE';
PRINT N'========================================';
PRINT N'Database: QL_DUANCANHAN_LITE';
PRINT N'Tài kho?n: admin@test.com / 123';
PRINT N'           user@test.com / 123';
PRINT N'========================================';
GO
