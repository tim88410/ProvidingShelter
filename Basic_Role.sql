/* ============================================================
   Server-level: �إ� SQL �n�J�]�Y��� AD�A�Х� CREATE LOGIN FROM WINDOWS�^
   �д����K�X���A�ۤv���j�K�X����
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
   Database-level: ������ؼи�Ʈw
   �ö}�� RCSI�]Ū�w�{�i�ַӡ^�P���\�㦡 Snapshot �j���]��Ρ^
   ============================================================ */
ALTER DATABASE [ProvidingShelter] SET ALLOW_SNAPSHOT_ISOLATION ON;
ALTER DATABASE [ProvidingShelter] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
GO

USE [ProvidingShelter];
GO

/* ============================================================
   �إ߹�������Ʈw�ϥΪ̡]�w�] schema �]�� dbo�^
   ============================================================ */
IF USER_ID('ProvidingShelter_Web_Reader') IS NULL
  CREATE USER [ProvidingShelter_Web_Reader] FOR LOGIN [ProvidingShelter_Web_Reader] WITH DEFAULT_SCHEMA = [dbo];

IF USER_ID('ProvidingShelter_Web_Writer') IS NULL
  CREATE USER [ProvidingShelter_Web_Writer] FOR LOGIN [ProvidingShelter_Web_Writer] WITH DEFAULT_SCHEMA = [dbo];

IF USER_ID('ProvidingShelter_Migrator') IS NULL
  CREATE USER [ProvidingShelter_Migrator] FOR LOGIN [ProvidingShelter_Migrator] WITH DEFAULT_SCHEMA = [dbo];

IF USER_ID('ProvidingShelter_Job_Writer') IS NULL
  CREATE USER [ProvidingShelter_Job_Writer] FOR LOGIN [ProvidingShelter_Job_Writer] WITH DEFAULT_SCHEMA = [dbo];

-- �]�i��^���T�¤� CONNECT
GRANT CONNECT TO [ProvidingShelter_Web_Reader];
GRANT CONNECT TO [ProvidingShelter_Web_Writer];
GRANT CONNECT TO [ProvidingShelter_Migrator];
GRANT CONNECT TO [ProvidingShelter_Job_Writer];

GO
/* ============================================================
   �v���G�� schema �ű��v���u�s��۰ʦ��v���v
   * ���]�j�a���b dbo schema �ت�F�Y���ۭq schema�A�й墨�� schema �]�[�W���� GRANT
   ============================================================ */

-- 1) ��Ū�GProvidingShelter_Web_Reader
GRANT SELECT ON SCHEMA::[dbo] TO [ProvidingShelter_Web_Reader];
-- �Y�ݭnŪ���˵�/�禡�άd�ߥ� SP�A�@�ֹ� schema ���v�G
GRANT EXECUTE ON SCHEMA::[dbo] TO [ProvidingShelter_Web_Reader];

-- 2) �i�g�GProvidingShelter_Web_Writer�]�t MERGE �һݪ� DML �v���^
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[dbo] TO [ProvidingShelter_Web_Writer];
GRANT EXECUTE ON SCHEMA::[dbo] TO [ProvidingShelter_Web_Writer];  -- �i����s�إߪ� SP/�禡

-- 3) Migrator�Gdb_ddladmin�]���c�ܧ�^
EXEC sp_addrolemember  @rolename = 'db_ddladmin', @membername = 'ProvidingShelter_Migrator';
-- �]�i��^�Y�ݭn�P�ɯ�b���p�}�����d���ơA�]�i�� SELECT/EXECUTE
GRANT SELECT ON SCHEMA::[dbo] TO [ProvidingShelter_Migrator];
GRANT EXECUTE ON SCHEMA::[dbo] TO [ProvidingShelter_Migrator];

-- 4) �妸�g�J�b���]�P Web_Writer �P�v���^
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[dbo] TO [ProvidingShelter_Job_Writer];
GRANT EXECUTE ON SCHEMA::[dbo] TO [ProvidingShelter_Job_Writer];

GO
/* ============================================================
   ���ҡ]��Ρ^�G�μ����ˬd�v���O�_�N��
   ============================================================ */
-- Reader�G���ӯ� SELECT�B���� INSERT
EXECUTE AS USER = 'ProvidingShelter_Web_Reader';
SELECT HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'SELECT')  AS Reader_Select_Schema;   -- 1=OK
SELECT HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'INSERT')  AS Reader_Insert_Schema;   -- 0=OK(���Ӧ�)
REVERT;

-- Writer�G���Ӧ� SELECT/INSERT/UPDATE/DELETE
EXECUTE AS USER = 'ProvidingShelter_Web_Writer';
SELECT HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'SELECT')  AS Writer_Select_Schema;   -- 1
SELECT HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'INSERT')  AS Writer_Insert_Schema;   -- 1
SELECT HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'UPDATE')  AS Writer_Update_Schema;   -- 1
SELECT HAS_PERMS_BY_NAME('dbo', 'SCHEMA', 'DELETE')  AS Writer_Delete_Schema;   -- 1
REVERT;

-- Migrator�G���� db_ddladmin ����
SELECT 'db_ddladmin' AS role, IS_MEMBER('db_ddladmin') AS is_member_for_migrator
WHERE USER_NAME() = 'ProvidingShelter_Migrator';
