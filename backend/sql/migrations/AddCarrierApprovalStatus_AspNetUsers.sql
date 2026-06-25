-- Adds carrier vetting field to ASP.NET Identity users (run once per database).
-- Safe to run multiple times if column already exists (SQL Server 2016+).

IF COL_LENGTH('dbo.AspNetUsers', 'CarrierApprovalStatus') IS NULL
BEGIN
    ALTER TABLE dbo.AspNetUsers ADD CarrierApprovalStatus NVARCHAR(32) NULL;
END
GO
