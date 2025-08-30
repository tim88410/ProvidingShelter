/* ============================================================
   Server-level: 建立 SQL 登入（若改用 AD，請用 CREATE LOGIN FROM WINDOWS）
   請替換密碼為你自己的強密碼策略
   ============================================================ */
IF SUSER_ID('ProvidingShelter_Web_Reader') IS NULL
  CREATE LOGIN [ProvidingShelter_Web_Reader] WITH PASSWORD = 'Strong!Reader#Pwd';
IF SUSER_ID('ProvidingShelter_Web_Writer') IS NULL
  CREATE LOGIN [ProvidingShelter_Web_Writer] WITH PASSWORD = 'Strong!Writer#Pwd';
IF SUSER_ID('ProvidingShelter_Migrator') IS NULL
  CREATE LOGIN [ProvidingShelter_Migrator] WITH PASSWORD = 'Strong!Migrator#Pwd';
IF SUSER_ID('ProvidingShelter_Job_Writer') IS NULL
  CREATE LOGIN [ProvidingShelter_Job_Writer] WITH PASSWORD = 'Strong!Job#Pwd';

GO
/* ============================================================
   Database-level: 切換到目標資料庫
   並開啟 RCSI（讀已認可快照）與允許顯式 Snapshot 隔離（選用）
   ============================================================ */
ALTER DATABASE [ProvidingShelter] SET ALLOW_SNAPSHOT_ISOLATION ON;
ALTER DATABASE [ProvidingShelter] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
GO

USE [ProvidingShelter];
GO

/* ============================================================
   建立對應的資料庫使用者（預設 schema 設為 dbo）
   ============================================================ */
IF USER_ID('ProvidingShelter_Web_Reader') IS NULL
  CREATE USER [ProvidingShelter_Web_Reader] FOR LOGIN [ProvidingShelter_Web_Reader] WITH DEFAULT_SCHEMA = [dbo];

IF USER_ID('ProvidingShelter_Web_Writer') IS NULL
  CREATE USER [ProvidingShelter_Web_Writer] FOR LOGIN [ProvidingShelter_Web_Writer] WITH DEFAULT_SCHEMA = [dbo];

IF USER_ID('ProvidingShelter_Migrator') IS NULL
  CREATE USER [ProvidingShelter_Migrator] FOR LOGIN [ProvidingShelter_Migrator] WITH DEFAULT_SCHEMA = [dbo];

IF USER_ID('ProvidingShelter_Job_Writer') IS NULL
  CREATE USER [ProvidingShelter_Job_Writer] FOR LOGIN [ProvidingShelter_Job_Writer] WITH DEFAULT_SCHEMA = [dbo];

-- （可選）明確授予 CONNECT
GRANT CONNECT TO [ProvidingShelter_Web_Reader];
GRANT CONNECT TO [ProvidingShelter_Web_Writer];
GRANT CONNECT TO [ProvidingShelter_Migrator];
GRANT CONNECT TO [ProvidingShelter_Job_Writer];

GO
/* ============================================================
   權限：用 schema 級授權讓「新表自動有權限」
   * 假設大家都在 dbo schema 建表；若有自訂 schema，請對那些 schema 也加上等價 GRANT
   ============================================================ */

-- 1) 僅讀：ProvidingShelter_Web_Reader
GRANT SELECT ON SCHEMA::[dbo] TO [ProvidingShelter_Web_Reader];
-- 若需要讀取檢視/函式或查詢用 SP，一併對 schema 授權：
GRANT EXECUTE ON SCHEMA::[dbo] TO [ProvidingShelter_Web_Reader];

-- 2) 可寫：ProvidingShelter_Web_Writer（含 MERGE 所需的 DML 權限）
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[dbo] TO [ProvidingShelter_Web_Writer];
GRANT EXECUTE ON SCHEMA::[dbo] TO [ProvidingShelter_Web_Writer];  -- 可執行新建立的 SP/函式

-- 3) Migrator：db_ddladmin（結構變更）
EXEC sp_addrolemember  @rolename = 'db_ddladmin', @membername = 'ProvidingShelter_Migrator';
-- （可選）若需要同時能在部署腳本中查驗資料，也可給 SELECT/EXECUTE
GRANT SELECT ON SCHEMA::[dbo] TO [ProvidingShelter_Migrator];
GRANT EXECUTE ON SCHEMA::[dbo] TO [ProvidingShelter_Migrator];

-- 4) 批次寫入帳號（與 Web_Writer 同權限）
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[dbo] TO [ProvidingShelter_Job_Writer];
GRANT EXECUTE ON SCHEMA::[dbo] TO [ProvidingShelter_Job_Writer];

GO
/* ============================================================
   驗證（選用）：用模擬檢查權限是否就緒
   ============================================================ */
-- Reader：應該能 SELECT、不能 INSERT
EXECUTE AS USER = 'ProvidingShelter_Web_Reader';
SELECT HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'SELECT')  AS Reader_Select_Schema;   -- 1=OK
SELECT HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'INSERT')  AS Reader_Insert_Schema;   -- 0=OK(不該有)
REVERT;

-- Writer：應該有 SELECT/INSERT/UPDATE/DELETE
EXECUTE AS USER = 'ProvidingShelter_Web_Writer';
SELECT HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'SELECT')  AS Writer_Select_Schema;   -- 1
SELECT HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'INSERT')  AS Writer_Insert_Schema;   -- 1
SELECT HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'UPDATE')  AS Writer_Update_Schema;   -- 1
SELECT HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'DELETE')  AS Writer_Delete_Schema;   -- 1
REVERT;

-- Migrator：應為 db_ddladmin 成員
SELECT 'db_ddladmin' AS role, IS_MEMBER('db_ddladmin') AS is_member_for_migrator
WHERE USER_NAME() = 'ProvidingShelter_Migrator';
