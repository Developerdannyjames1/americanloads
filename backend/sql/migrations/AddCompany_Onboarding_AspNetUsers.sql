-- Company entity + AspNetUsers.CompanyId (Identity / ASP.NET)
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Companies' AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[Companies] (
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Name] [nvarchar](256) NOT NULL,
        [CompanyType] [nvarchar](32) NOT NULL,
        [OnboardingStatus] [nvarchar](32) NULL,
        [CreatedUtc] [datetime2](7) NOT NULL DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_Companies] PRIMARY KEY ([Id])
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'CompanyId')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [CompanyId] [int] NULL;
END
GO
