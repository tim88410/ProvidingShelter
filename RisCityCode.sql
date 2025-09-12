SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;

-- �������J���P�W��ƪ�
IF OBJECT_ID(N'dbo.RisCityCode', N'U') IS NOT NULL
    DROP TABLE dbo.RisCityCode;

-- �̻ݨD�إߡGResourceUrl / CityCode(�N�X) / CityName(����W)
CREATE TABLE dbo.RisCityCode
(
    ResourceUrl NVARCHAR(200) NOT NULL,
    CityCode    NVARCHAR(10)  NOT NULL,  -- �N�X�]�O�d�e�� 0�^
    CityName    NVARCHAR(20)  NOT NULL,  -- ����W��
	IsCurrent   BIT			  NULL, --�O�_�̷s
    CONSTRAINT PK_RisCityCode PRIMARY KEY CLUSTERED (CityCode)
);

-- ��ƨӷ����}�]�Τ@�^
DECLARE @url NVARCHAR(200) = N'https://www.ris.gov.tw/documents/html/5/1/168.html';

-- �g�J��ơ]�N�X���W�١^
INSERT INTO dbo.RisCityCode (ResourceUrl, CityCode, CityName)
VALUES
(@url, N'10001', N'�O�_��'),
(@url, N'10002', N'�y����'),
(@url, N'10003', N'��鿤'),
(@url, N'10004', N'�s�˿�'),
(@url, N'10005', N'�]�߿�'),
(@url, N'10006', N'�O����'),
(@url, N'10007', N'���ƿ�'),
(@url, N'10008', N'�n�뿤'),
(@url, N'10009', N'���L��'),
(@url, N'10010', N'�Ÿq��'),
(@url, N'10011', N'�O�n��'),
(@url, N'10012', N'������'),
(@url, N'10013', N'�̪F��'),
(@url, N'10014', N'�O�F��'),
(@url, N'10015', N'�Ὤ��'),
(@url, N'10016', N'���'),
(@url, N'10017', N'�򶩥�'),
(@url, N'10018', N'�s�˥�'),
(@url, N'10019', N'�O����'),
(@url, N'10020', N'�Ÿq��'),
(@url, N'10021', N'�O�n��'),
(@url, N'09007', N'�s����'),
(@url, N'09020', N'������'),
(@url, N'63000', N'�O�_��'),
(@url, N'64000', N'������'),
(@url, N'65000', N'�s�_��'),
(@url, N'66000', N'�O����'),
(@url, N'67000', N'�O�n��'),
(@url, N'68000', N'��饫');

COMMIT TRAN;
