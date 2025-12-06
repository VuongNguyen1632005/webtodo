
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'QL_DUANCANHAN_LITE')
BEGIN
    CREATE DATABASE QL_DUANCANHAN_LITE;
END
GO

USE QL_DUANCANHAN_LITE;
GO


IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TaiKhoan')
BEGIN
    CREATE TABLE TaiKhoan (
        MaTaiKhoan INT IDENTITY(1,1) PRIMARY KEY,
        DiaChiEmail NVARCHAR(255) NOT NULL UNIQUE,
        MatKhau NVARCHAR(255) NOT NULL,
        HoTen NVARCHAR(100) NULL,
        AnhDaiDien NVARCHAR(500) NULL
    );
    PRINT N'Đã tạo bảng TaiKhoan';
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Bang')
BEGIN
    CREATE TABLE Bang (
        MaBang INT IDENTITY(1,1) PRIMARY KEY,
        MaNguoiSoHuu INT NOT NULL,
        TenBang NVARCHAR(255) NOT NULL,
        MauNen NVARCHAR(50) NULL DEFAULT '#0079bf',
        NgayTao DATETIME NULL DEFAULT GETDATE(),
        
        CONSTRAINT FK_Bang_TaiKhoan FOREIGN KEY (MaNguoiSoHuu) 
            REFERENCES TaiKhoan(MaTaiKhoan) ON DELETE NO ACTION
    );
    PRINT N'Đã tạo bảng Bang';
END
GO


IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Cot')
BEGIN
    CREATE TABLE Cot (
        MaCot INT IDENTITY(1,1) PRIMARY KEY,
        MaBang INT NOT NULL,
        TenCot NVARCHAR(255) NOT NULL,
        ThuTu INT NOT NULL DEFAULT 0,
        KichHoat BIT NULL DEFAULT 1,
        
        CONSTRAINT FK_Cot_Bang FOREIGN KEY (MaBang) 
            REFERENCES Bang(MaBang) ON DELETE CASCADE
    );
    PRINT N'Đã tạo bảng Cot';
END
GO


IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'The')
BEGIN
    CREATE TABLE The (
        MaThe INT IDENTITY(1,1) PRIMARY KEY,
        MaCot INT NOT NULL,
        TieuDe NVARCHAR(500) NOT NULL,
        MoTa NVARCHAR(MAX) NULL,
        HanChot DATETIME NULL,
        DaHoanThanh BIT NULL DEFAULT 0,
        ThuTu INT NOT NULL DEFAULT 0,
        
        CONSTRAINT FK_The_Cot FOREIGN KEY (MaCot) 
            REFERENCES Cot(MaCot) ON DELETE CASCADE
    );
    PRINT N'Đã tạo bảng The';
END
GO


IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GhiChu')
BEGIN
    CREATE TABLE GhiChu (
        MaGhiChu INT IDENTITY(1,1) PRIMARY KEY,
        MaThe INT NOT NULL,
        MaTaiKhoan INT NULL,
        NoiDung NVARCHAR(MAX) NOT NULL,
        NgayTao DATETIME NULL DEFAULT GETDATE(),
        
        CONSTRAINT FK_GhiChu_The FOREIGN KEY (MaThe) 
            REFERENCES The(MaThe) ON DELETE CASCADE,
        CONSTRAINT FK_GhiChu_TaiKhoan FOREIGN KEY (MaTaiKhoan) 
            REFERENCES TaiKhoan(MaTaiKhoan) ON DELETE SET NULL
    );
    PRINT N'Đã tạo bảng GhiChu';
END
GO


IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MucTieu')
BEGIN
    CREATE TABLE MucTieu (
        MaMucTieu INT IDENTITY(1,1) PRIMARY KEY,
        MaThe INT NOT NULL,
        TenMucTieu NVARCHAR(500) NOT NULL,
        DaKiemTra BIT NULL DEFAULT 0,
        ThuTu INT NOT NULL DEFAULT 0,
        
        CONSTRAINT FK_MucTieu_The FOREIGN KEY (MaThe) 
            REFERENCES The(MaThe) ON DELETE CASCADE
    );
    PRINT N'Đã tạo bảng MucTieu';
END
GO


IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ThanhVienBang')
BEGIN
    CREATE TABLE ThanhVienBang (
        MaBang INT NOT NULL,
        MaTaiKhoan INT NOT NULL,
        VaiTro NVARCHAR(50) NOT NULL DEFAULT 'viewer', -- 'owner', 'editor', 'viewer'
        NgayThamGia DATETIME NULL DEFAULT GETDATE(),
        
        CONSTRAINT PK_ThanhVienBang PRIMARY KEY (MaBang, MaTaiKhoan),
        CONSTRAINT FK_ThanhVienBang_Bang FOREIGN KEY (MaBang) 
            REFERENCES Bang(MaBang) ON DELETE CASCADE,
        CONSTRAINT FK_ThanhVienBang_TaiKhoan FOREIGN KEY (MaTaiKhoan) 
            REFERENCES TaiKhoan(MaTaiKhoan) ON DELETE NO ACTION
    );
    PRINT N'Đã tạo bảng ThanhVienBang';
END
GO


IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NhanCuaBang')
BEGIN
    CREATE TABLE NhanCuaBang (
        MaNhanCuaBang INT IDENTITY(1,1) PRIMARY KEY,
        MaBang INT NOT NULL,
        TenHienThi NVARCHAR(100) NOT NULL,
        MaMau NVARCHAR(20) NOT NULL DEFAULT '#61bd4f',
        
        CONSTRAINT FK_NhanCuaBang_Bang FOREIGN KEY (MaBang) 
            REFERENCES Bang(MaBang) ON DELETE CASCADE
    );
    PRINT N'Đã tạo bảng NhanCuaBang';
END
GO


IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NhanCuaThe')
BEGIN
    CREATE TABLE NhanCuaThe (
        MaNhanCuaThe INT IDENTITY(1,1) PRIMARY KEY,
        MaNhanCuaBang INT NOT NULL,
        MaThe INT NOT NULL,
        
        CONSTRAINT FK_NhanCuaThe_NhanCuaBang FOREIGN KEY (MaNhanCuaBang) 
            REFERENCES NhanCuaBang(MaNhanCuaBang) ON DELETE NO ACTION,
        CONSTRAINT FK_NhanCuaThe_The FOREIGN KEY (MaThe) 
            REFERENCES The(MaThe) ON DELETE CASCADE,
        CONSTRAINT UQ_NhanCuaThe UNIQUE (MaNhanCuaBang, MaThe)
    );
    PRINT N'Đã tạo bảng NhanCuaThe';
END
GO


IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ThanhVienCuaThe')
BEGIN
    CREATE TABLE ThanhVienCuaThe (
        MaThanhVienCuaThe INT IDENTITY(1,1) PRIMARY KEY,
        MaThe INT NOT NULL,
        MaTaiKhoan INT NOT NULL,
        NgayGan DATETIME NULL DEFAULT GETDATE(),
        
        CONSTRAINT FK_ThanhVienCuaThe_The FOREIGN KEY (MaThe) 
            REFERENCES The(MaThe) ON DELETE CASCADE,
        CONSTRAINT FK_ThanhVienCuaThe_TaiKhoan FOREIGN KEY (MaTaiKhoan) 
            REFERENCES TaiKhoan(MaTaiKhoan) ON DELETE NO ACTION,
        CONSTRAINT UQ_ThanhVienCuaThe UNIQUE (MaThe, MaTaiKhoan)
    );
    PRINT N'Đã tạo bảng ThanhVienCuaThe';
END
GO



-- Index cho bảng Bang
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Bang_MaNguoiSoHuu')
    CREATE INDEX IX_Bang_MaNguoiSoHuu ON Bang(MaNguoiSoHuu);

-- Index cho bảng Cot
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Cot_MaBang')
    CREATE INDEX IX_Cot_MaBang ON Cot(MaBang);

-- Index cho bảng The
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_The_MaCot')
    CREATE INDEX IX_The_MaCot ON The(MaCot);

-- Index cho bảng GhiChu
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_GhiChu_MaThe')
    CREATE INDEX IX_GhiChu_MaThe ON GhiChu(MaThe);

-- Index cho bảng MucTieu
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MucTieu_MaThe')
    CREATE INDEX IX_MucTieu_MaThe ON MucTieu(MaThe);

-- Index cho bảng NhanCuaBang
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NhanCuaBang_MaBang')
    CREATE INDEX IX_NhanCuaBang_MaBang ON NhanCuaBang(MaBang);

-- Index cho bảng NhanCuaThe
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NhanCuaThe_MaThe')
    CREATE INDEX IX_NhanCuaThe_MaThe ON NhanCuaThe(MaThe);

-- Index cho bảng ThanhVienCuaThe
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ThanhVienCuaThe_MaThe')
    CREATE INDEX IX_ThanhVienCuaThe_MaThe ON ThanhVienCuaThe(MaThe);

PRINT N'Đã tạo các Index';
GO


IF NOT EXISTS (SELECT * FROM TaiKhoan WHERE DiaChiEmail = 'admin@example.com')
BEGIN
    INSERT INTO TaiKhoan (DiaChiEmail, MatKhau, HoTen)
    VALUES ('admin@example.com', '123456', N'Admin');
    PRINT N'Đã tạo tài khoản admin mẫu';
END
GO

-- T?o b?ng m?u cho admin
DECLARE @AdminId INT = (SELECT MaTaiKhoan FROM TaiKhoan WHERE DiaChiEmail = 'admin@example.com');

IF @AdminId IS NOT NULL AND NOT EXISTS (SELECT * FROM Bang WHERE MaNguoiSoHuu = @AdminId)
BEGIN
    -- T?o b?ng
    INSERT INTO Bang (MaNguoiSoHuu, TenBang, MauNen)
    VALUES (@AdminId, N'Dự án mẫu', '#0079bf');
    
    DECLARE @BangId INT = SCOPE_IDENTITY();
    
    -- T?o các c?t m?c ??nh
    INSERT INTO Cot (MaBang, TenCot, ThuTu, KichHoat) VALUES (@BangId, N'Cần làm', 0, 1);
    INSERT INTO Cot (MaBang, TenCot, ThuTu, KichHoat) VALUES (@BangId, N'Đang làm', 1, 1);
    INSERT INTO Cot (MaBang, TenCot, ThuTu, KichHoat) VALUES (@BangId, N'Đã xong', 2, 1);
    
    -- T?o nhãn m?u
    INSERT INTO NhanCuaBang (MaBang, TenHienThi, MaMau) VALUES (@BangId, N'Gấp', '#eb5a46');
    INSERT INTO NhanCuaBang (MaBang, TenHienThi, MaMau) VALUES (@BangId, N'Quan trọng', '#f2d600');
    INSERT INTO NhanCuaBang (MaBang, TenHienThi, MaMau) VALUES (@BangId, N'Bình thường', '#61bd4f');
    
    PRINT N'Đã tạo dữ liệu mẫu';
END
GO


