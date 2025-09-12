SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

-- 先移除既有同名資料表
IF OBJECT_ID(N'dbo.RisCityCode', N'U') IS NOT NULL
    DROP TABLE dbo.RisCityCode;

-- 依需求建立：ResourceUrl / CityCode(代碼) / CityName(中文名)
CREATE TABLE dbo.RisCityCode
(
    ResourceUrl NVARCHAR(200) NOT NULL,
    CityCode    NVARCHAR(10)  NOT NULL,  -- 代碼（保留前導 0）
    CityName    NVARCHAR(20)  NOT NULL,  -- 中文名稱
	IsCurrent   BIT			  NULL, --是否最新
    CONSTRAINT PK_RisCityCode PRIMARY KEY CLUSTERED (CityCode)
);

-- 資料來源網址（統一）
DECLARE @url NVARCHAR(200) = N'https://www.ris.gov.tw/documents/html/5/1/168.html';

-- 寫入資料（代碼→名稱）
INSERT INTO dbo.RisCityCode (ResourceUrl, CityCode, CityName)
VALUES
(@url, N'10001', N'臺北縣'),
(@url, N'10002', N'宜蘭縣'),
(@url, N'10003', N'桃園縣'),
(@url, N'10004', N'新竹縣'),
(@url, N'10005', N'苗栗縣'),
(@url, N'10006', N'臺中縣'),
(@url, N'10007', N'彰化縣'),
(@url, N'10008', N'南投縣'),
(@url, N'10009', N'雲林縣'),
(@url, N'10010', N'嘉義縣'),
(@url, N'10011', N'臺南縣'),
(@url, N'10012', N'高雄縣'),
(@url, N'10013', N'屏東縣'),
(@url, N'10014', N'臺東縣'),
(@url, N'10015', N'花蓮縣'),
(@url, N'10016', N'澎湖縣'),
(@url, N'10017', N'基隆市'),
(@url, N'10018', N'新竹市'),
(@url, N'10019', N'臺中市'),
(@url, N'10020', N'嘉義市'),
(@url, N'10021', N'臺南市'),
(@url, N'09007', N'連江縣'),
(@url, N'09020', N'金門縣'),
(@url, N'63000', N'臺北市'),
(@url, N'64000', N'高雄市'),
(@url, N'65000', N'新北市'),
(@url, N'66000', N'臺中市'),
(@url, N'67000', N'臺南市'),
(@url, N'68000', N'桃園市');

COMMIT TRAN;
