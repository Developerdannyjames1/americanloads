IF OBJECT_ID(N'dbo.LoadTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LoadTemplates
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(120) NOT NULL,
        IsGlobal BIT NOT NULL CONSTRAINT DF_LoadTemplates_IsGlobal DEFAULT(0),
        CompanyId INT NULL,
        LoadTypeId INT NULL,
        AssetLength INT NULL,
        Weight INT NULL,
        OriginId INT NULL,
        DestinationId INT NULL,
        OriginCity NVARCHAR(200) NULL,
        OriginState NVARCHAR(10) NULL,
        DestinationCity NVARCHAR(200) NULL,
        DestinationState NVARCHAR(10) NULL,
        Notes NVARCHAR(1000) NULL,
        CreatedByUserId NVARCHAR(128) NULL
    );

    CREATE INDEX IX_LoadTemplates_CompanyId ON dbo.LoadTemplates(CompanyId);
    CREATE INDEX IX_LoadTemplates_IsGlobal ON dbo.LoadTemplates(IsGlobal);
END
GO

IF COL_LENGTH('dbo.LoadTemplates', 'OriginCity') IS NULL
    ALTER TABLE dbo.LoadTemplates ADD OriginCity NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.LoadTemplates', 'OriginState') IS NULL
    ALTER TABLE dbo.LoadTemplates ADD OriginState NVARCHAR(10) NULL;
IF COL_LENGTH('dbo.LoadTemplates', 'DestinationCity') IS NULL
    ALTER TABLE dbo.LoadTemplates ADD DestinationCity NVARCHAR(200) NULL;
IF COL_LENGTH('dbo.LoadTemplates', 'DestinationState') IS NULL
    ALTER TABLE dbo.LoadTemplates ADD DestinationState NVARCHAR(10) NULL;
GO
