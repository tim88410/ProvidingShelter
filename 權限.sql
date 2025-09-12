/* === 0) �� master �� Login�]�Y���s�b�^ === */
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

/* === 1) ����ؼ� DB�]�Х��T�w DB �w�s�b�^ === */
IF DB_ID(N'ProvidingShelter') IS NULL
BEGIN
    RAISERROR(N'Database [ProvidingShelter] not found.',16,1);
    RETURN;
END
GO
USE [ProvidingShelter];
GO

/* === 2) �� DB User�]�Y���s�b�^�ADefault Schema = dbo === */
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'ProvidingShelter_Web_Reader')
    CREATE USER [ProvidingShelter_Web_Reader] FOR LOGIN [ProvidingShelter_Web_Reader] WITH DEFAULT_SCHEMA = [dbo];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'ProvidingShelter_Web_Writer')
    CREATE USER [ProvidingShelter_Web_Writer] FOR LOGIN [ProvidingShelter_Web_Writer] WITH DEFAULT_SCHEMA = [dbo];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'ProvidingShelter_Job_Writer')
    CREATE USER [ProvidingShelter_Job_Writer] FOR LOGIN [ProvidingShelter_Job_Writer] WITH DEFAULT_SCHEMA = [dbo];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'ProvidingShelter_Migrator')
    CREATE USER [ProvidingShelter_Migrator] FOR LOGIN [ProvidingShelter_Migrator] WITH DEFAULT_SCHEMA = [dbo];
GO

/* === 3) ��������]�Ҩ�� idempotent �P�_�^ === */

/* Reader�G��Ū + ���T�T��g */
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

/* Web Writer�G�iŪ�g�]�L DDL �v���^ */
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

/* Job Writer�G�妸�g�J�A�P Web Writer �v�� */
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

/* Migrator�G���� EF Migrations�]�� DB �� db_owner�^ */
IF NOT EXISTS (
    SELECT 1 FROM sys.database_role_members rm
    JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
    JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
    WHERE r.name = N'db_owner' AND m.name = N'ProvidingShelter_Migrator'
)
    ALTER ROLE [db_owner] ADD MEMBER [ProvidingShelter_Migrator];
GO
