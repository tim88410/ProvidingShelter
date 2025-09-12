/* === 0) 於 master 建 Login（若不存在） === */
USE [master];
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'ProvidingShelter_Web_Reader')
    CREATE LOGIN [ProvidingShelter_Web_Reader] WITH PASSWORD = N'Strong!Reader#Pwd', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'ProvidingShelter_Web_Writer')
    CREATE LOGIN [ProvidingShelter_Web_Writer] WITH PASSWORD = N'Strong!Writer#Pwd', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'ProvidingShelter_Job_Writer')
    CREATE LOGIN [ProvidingShelter_Job_Writer] WITH PASSWORD = N'Strong!Job#Pwd', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'ProvidingShelter_Migrator')
    CREATE LOGIN [ProvidingShelter_Migrator] WITH PASSWORD = N'Strong!Migrator#Pwd', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;
GO

/* === 1) 切到目標 DB（請先確定 DB 已存在） === */
IF DB_ID(N'ProvidingShelter') IS NULL
BEGIN
    RAISERROR(N'Database [ProvidingShelter] not found.',16,1);
    RETURN;
END
GO
USE [ProvidingShelter];
GO

/* === 2) 建 DB User（若不存在），Default Schema = dbo === */
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'ProvidingShelter_Web_Reader')
    CREATE USER [ProvidingShelter_Web_Reader] FOR LOGIN [ProvidingShelter_Web_Reader] WITH DEFAULT_SCHEMA = [dbo];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'ProvidingShelter_Web_Writer')
    CREATE USER [ProvidingShelter_Web_Writer] FOR LOGIN [ProvidingShelter_Web_Writer] WITH DEFAULT_SCHEMA = [dbo];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'ProvidingShelter_Job_Writer')
    CREATE USER [ProvidingShelter_Job_Writer] FOR LOGIN [ProvidingShelter_Job_Writer] WITH DEFAULT_SCHEMA = [dbo];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'ProvidingShelter_Migrator')
    CREATE USER [ProvidingShelter_Migrator] FOR LOGIN [ProvidingShelter_Migrator] WITH DEFAULT_SCHEMA = [dbo];
GO

/* === 3) 指派角色（皆具備 idempotent 判斷） === */

/* Reader：唯讀 + 明確禁止寫 */
IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members rm
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
    WHERE r.name = N'db_datareader' AND m.name = N'ProvidingShelter_Web_Reader'
)
    ALTER ROLE [db_datareader] ADD MEMBER [ProvidingShelter_Web_Reader];

IF NOT EXISTS (
    SELECT 1
    FROM sys.database_role_members rm
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
    WHERE r.name = N'db_denydatawriter' AND m.name = N'ProvidingShelter_Web_Reader'
)
    ALTER ROLE [db_denydatawriter] ADD MEMBER [ProvidingShelter_Web_Reader];

/* Web Writer：可讀寫（無 DDL 權限） */
IF NOT EXISTS (
    SELECT 1 FROM sys.database_role_members rm
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
    WHERE r.name = N'db_datareader' AND m.name = N'ProvidingShelter_Web_Writer'
)
    ALTER ROLE [db_datareader] ADD MEMBER [ProvidingShelter_Web_Writer];

IF NOT EXISTS (
    SELECT 1 FROM sys.database_role_members rm
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
    WHERE r.name = N'db_datawriter' AND m.name = N'ProvidingShelter_Web_Writer'
)
    ALTER ROLE [db_datawriter] ADD MEMBER [ProvidingShelter_Web_Writer];

/* Job Writer：批次寫入，同 Web Writer 權限 */
IF NOT EXISTS (
    SELECT 1 FROM sys.database_role_members rm
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
    WHERE r.name = N'db_datareader' AND m.name = N'ProvidingShelter_Job_Writer'
)
    ALTER ROLE [db_datareader] ADD MEMBER [ProvidingShelter_Job_Writer];

IF NOT EXISTS (
    SELECT 1 FROM sys.database_role_members rm
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
    WHERE r.name = N'db_datawriter' AND m.name = N'ProvidingShelter_Job_Writer'
)
    ALTER ROLE [db_datawriter] ADD MEMBER [ProvidingShelter_Job_Writer];

/* Migrator：執行 EF Migrations（本 DB 給 db_owner） */
IF NOT EXISTS (
    SELECT 1 FROM sys.database_role_members rm
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
    WHERE r.name = N'db_owner' AND m.name = N'ProvidingShelter_Migrator'
)
    ALTER ROLE [db_owner] ADD MEMBER [ProvidingShelter_Migrator];
GO
