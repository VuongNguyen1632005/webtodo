-- CreateDatabase_From_EDMX.sql
-- Generated from Model1.edmx (EF SSDL)
-- SQL Server 2012 compatible

IF DB_ID(N'QL_DUANCANHAN_LITE') IS NULL
BEGIN
    CREATE DATABASE [QL_DUANCANHAN_LITE];
    PRINT N'Created database QL_DUANCANHAN_LITE';
END
GO

USE [QL_DUANCANHAN_LITE];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Table: TaiKhoan
IF OBJECT_ID(N'dbo.TaiKhoan', N'U') IS NULL
BEGIN
CREATE TABLE dbo.TaiKhoan (
    MaTaiKhoan INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    DiaChiEmail NVARCHAR(100) NOT NULL,
    MatKhau NVARCHAR(255) NOT NULL,
    HoTen NVARCHAR(100) NULL,
    AnhDaiDien NVARCHAR(500) NULL
);
END
GO

-- Table: Bang
IF OBJECT_ID(N'dbo.Bang', N'U') IS NULL
BEGIN
CREATE TABLE dbo.Bang (
    MaBang INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MaNguoiSoHuu INT NOT NULL,
    TenBang NVARCHAR(100) NOT NULL,
    MauNen NVARCHAR(20) NULL,
    NgayTao DATETIME NULL
);
END
GO

-- Table: Cot
IF OBJECT_ID(N'dbo.Cot', N'U') IS NULL
BEGIN
CREATE TABLE dbo.Cot (
    MaCot INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MaBang INT NOT NULL,
    TenCot NVARCHAR(100) NOT NULL,
    ThuTu INT NOT NULL,
    KichHoat BIT NULL
);
END
GO

-- Table: The
IF OBJECT_ID(N'dbo.The', N'U') IS NULL
BEGIN
CREATE TABLE dbo.The (
    MaThe INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MaCot INT NOT NULL,
    TieuDe NVARCHAR(255) NOT NULL,
    MoTa NVARCHAR(MAX) NULL,
    HanChot DATETIME NULL,
    DaHoanThanh BIT NULL,
    ThuTu INT NOT NULL
);
END
GO

-- Table: GhiChu
IF OBJECT_ID(N'dbo.GhiChu', N'U') IS NULL
BEGIN
CREATE TABLE dbo.GhiChu (
    MaGhiChu INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MaThe INT NOT NULL,
    NoiDung NVARCHAR(1000) NOT NULL,
    NgayTao DATETIME NULL,
    MaTaiKhoan INT NULL
);
END
GO

-- Table: MucTieu
IF OBJECT_ID(N'dbo.MucTieu', N'U') IS NULL
BEGIN
CREATE TABLE dbo.MucTieu (
    MaMucTieu INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MaThe INT NOT NULL,
    TenMucTieu NVARCHAR(255) NOT NULL,
    DaKiemTra BIT NULL,
    ThuTu INT NOT NULL
);
END
GO

-- Table: NhanCuaBang
IF OBJECT_ID(N'dbo.NhanCuaBang', N'U') IS NULL
BEGIN
CREATE TABLE dbo.NhanCuaBang (
    MaNhanCuaBang INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MaBang INT NOT NULL,
    TenHienThi NVARCHAR(50) NULL,
    MaMau NVARCHAR(7) NOT NULL
);
END
GO

-- Table: NhanCuaThe
IF OBJECT_ID(N'dbo.NhanCuaThe', N'U') IS NULL
BEGIN
CREATE TABLE dbo.NhanCuaThe (
    MaNhanCuaBang INT NOT NULL,
    MaThe INT NOT NULL,
    CONSTRAINT PK_NhanCuaThe PRIMARY KEY (MaNhanCuaBang, MaThe)
);
END
GO

-- Table: ThanhVienBang
IF OBJECT_ID(N'dbo.ThanhVienBang', N'U') IS NULL
BEGIN
CREATE TABLE dbo.ThanhVienBang (
    MaBang INT NOT NULL,
    MaTaiKhoan INT NOT NULL,
    VaiTro NVARCHAR(20) NULL,
    NgayThamGia DATETIME NULL,
    CONSTRAINT PK_ThanhVienBang PRIMARY KEY (MaBang, MaTaiKhoan)
);
END
GO

-- Table: sysdiagrams (used by SQL Server Management Studio)
IF OBJECT_ID(N'dbo.sysdiagrams', N'U') IS NULL
BEGIN
CREATE TABLE dbo.sysdiagrams (
    name NVARCHAR(128) NOT NULL,
    principal_id INT NOT NULL,
    diagram_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    version INT NULL,
    definition VARBINARY(MAX) NULL
);
END
GO

-- Foreign keys
-- Bang.MaNguoiSoHuu -> TaiKhoan.MaTaiKhoan
IF OBJECT_ID(N'dbo.FK_Bang_TaiKhoan', N'F') IS NULL
BEGIN
ALTER TABLE dbo.Bang
ADD CONSTRAINT FK_Bang_TaiKhoan FOREIGN KEY (MaNguoiSoHuu) REFERENCES dbo.TaiKhoan(MaTaiKhoan) ON DELETE NO ACTION;
END
GO

-- Cot.MaBang -> Bang.MaBang (ON DELETE CASCADE)
IF OBJECT_ID(N'dbo.FK_Cot_Bang', N'F') IS NULL
BEGIN
ALTER TABLE dbo.Cot
ADD CONSTRAINT FK_Cot_Bang FOREIGN KEY (MaBang) REFERENCES dbo.Bang(MaBang) ON DELETE CASCADE;
END
GO

-- The.MaCot -> Cot.MaCot (ON DELETE CASCADE)
IF OBJECT_ID(N'dbo.FK_The_Cot', N'F') IS NULL
BEGIN
ALTER TABLE dbo.The
ADD CONSTRAINT FK_The_Cot FOREIGN KEY (MaCot) REFERENCES dbo.Cot(MaCot) ON DELETE CASCADE;
END
GO

-- GhiChu.MaThe -> The.MaThe (ON DELETE CASCADE)
IF OBJECT_ID(N'dbo.FK_GhiChu_The', N'F') IS NULL
BEGIN
ALTER TABLE dbo.GhiChu
ADD CONSTRAINT FK_GhiChu_The FOREIGN KEY (MaThe) REFERENCES dbo.The(MaThe) ON DELETE CASCADE;
END
GO

-- GhiChu.MaTaiKhoan -> TaiKhoan.MaTaiKhoan (nullable)
IF OBJECT_ID(N'dbo.FK_GhiChu_TaiKhoan', N'F') IS NULL
BEGIN
ALTER TABLE dbo.GhiChu
ADD CONSTRAINT FK_GhiChu_TaiKhoan FOREIGN KEY (MaTaiKhoan) REFERENCES dbo.TaiKhoan(MaTaiKhoan) ON DELETE NO ACTION;
END
GO

-- MucTieu.MaThe -> The.MaThe (ON DELETE CASCADE)
IF OBJECT_ID(N'dbo.FK_MucTieu_The', N'F') IS NULL
BEGIN
ALTER TABLE dbo.MucTieu
ADD CONSTRAINT FK_MucTieu_The FOREIGN KEY (MaThe) REFERENCES dbo.The(MaThe) ON DELETE CASCADE;
END
GO

-- NhanCuaBang.MaBang -> Bang.MaBang (ON DELETE CASCADE)
IF OBJECT_ID(N'dbo.FK_NhanCuaBang_Bang', N'F') IS NULL
BEGIN
ALTER TABLE dbo.NhanCuaBang
ADD CONSTRAINT FK_NhanCuaBang_Bang FOREIGN KEY (MaBang) REFERENCES dbo.Bang(MaBang) ON DELETE CASCADE;
END
GO

-- NhanCuaThe.MaThe -> The.MaThe (ON DELETE CASCADE)
IF OBJECT_ID(N'dbo.FK_NhanCuaThe_The', N'F') IS NULL
BEGIN
ALTER TABLE dbo.NhanCuaThe
ADD CONSTRAINT FK_NhanCuaThe_The FOREIGN KEY (MaThe) REFERENCES dbo.The(MaThe) ON DELETE CASCADE;
END
GO

-- NhanCuaThe.MaNhanCuaBang -> NhanCuaBang.MaNhanCuaBang (NO ACTION on delete per model)
IF OBJECT_ID(N'dbo.FK_NhanCuaThe_NhanCuaBang', N'F') IS NULL
BEGIN
ALTER TABLE dbo.NhanCuaThe
ADD CONSTRAINT FK_NhanCuaThe_NhanCuaBang FOREIGN KEY (MaNhanCuaBang) REFERENCES dbo.NhanCuaBang(MaNhanCuaBang) ON DELETE NO ACTION;
END
GO

-- ThanhVienBang.MaBang -> Bang.MaBang (ON DELETE CASCADE)
IF OBJECT_ID(N'dbo.FK_ThanhVienBang_Bang', N'F') IS NULL
BEGIN
ALTER TABLE dbo.ThanhVienBang
ADD CONSTRAINT FK_ThanhVienBang_Bang FOREIGN KEY (MaBang) REFERENCES dbo.Bang(MaBang) ON DELETE CASCADE;
END
GO

-- ThanhVienBang.MaTaiKhoan -> TaiKhoan.MaTaiKhoan
IF OBJECT_ID(N'dbo.FK_ThanhVienBang_TaiKhoan', N'F') IS NULL
BEGIN
ALTER TABLE dbo.ThanhVienBang
ADD CONSTRAINT FK_ThanhVienBang_TaiKhoan FOREIGN KEY (MaTaiKhoan) REFERENCES dbo.TaiKhoan(MaTaiKhoan) ON DELETE NO ACTION;
END
GO

PRINT N'Schema creation finished.';
GO
