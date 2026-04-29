-- Add missing Company columns (when Companies table was created with only Id/Name, etc.)
-- Run on the same DB as DefaultConnection (Identity), e.g. [Ast] on LocalDB
GO
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Companies' AND type = 'U')
BEGIN
    IF COL_LENGTH('dbo.Companies', 'CompanyType') IS NULL
        ALTER TABLE [dbo].[Companies] ADD [CompanyType] [nvarchar](32) NOT NULL CONSTRAINT [DF_Companies_CompanyType] DEFAULT ('Shipper');
    IF COL_LENGTH('dbo.Companies', 'OnboardingStatus') IS NULL
        ALTER TABLE [dbo].[Companies] ADD [OnboardingStatus] [nvarchar](32) NULL;
    IF COL_LENGTH('dbo.Companies', 'CreatedUtc') IS NULL
        ALTER TABLE [dbo].[Companies] ADD [CreatedUtc] [datetime2](7) NOT NULL CONSTRAINT [DF_Companies_CreatedUtc] DEFAULT (sysutcdatetime());
END
GO
PRINT 'FixCompanies_Columns_If_Missing: done.';
