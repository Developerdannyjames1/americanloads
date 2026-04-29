-- Load workflow + claims/bids (run against the same database as DefaultConnection / Loads table).

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Loads') AND name = N'WorkflowStatus')
    ALTER TABLE dbo.Loads ADD WorkflowStatus NVARCHAR(32) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Loads') AND name = N'ShipperUserId')
    ALTER TABLE dbo.Loads ADD ShipperUserId NVARCHAR(128) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Loads') AND name = N'AssignedCarrierUserId')
    ALTER TABLE dbo.Loads ADD AssignedCarrierUserId NVARCHAR(128) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Loads') AND name = N'Commodity')
    ALTER TABLE dbo.Loads ADD Commodity NVARCHAR(500) NULL;
GO

IF OBJECT_ID(N'dbo.LoadClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LoadClaims (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        LoadId INT NOT NULL,
        CarrierUserId NVARCHAR(128) NOT NULL,
        ClaimType NVARCHAR(16) NOT NULL,
        BidAmount DECIMAL(18,2) NULL,
        Message NVARCHAR(2000) NULL,
        Status NVARCHAR(32) NOT NULL CONSTRAINT DF_LoadClaims_Status DEFAULT (N'pending'),
        CreatedUtc DATETIME2 NOT NULL CONSTRAINT DF_LoadClaims_CreatedUtc DEFAULT (SYSUTCDATETIME()),
        ResolvedUtc DATETIME2 NULL,
        ResolvedByUserId NVARCHAR(128) NULL,
        CONSTRAINT FK_LoadClaims_Loads FOREIGN KEY (LoadId) REFERENCES dbo.Loads(Id)
    );
    CREATE INDEX IX_LoadClaims_LoadId ON dbo.LoadClaims(LoadId);
    CREATE INDEX IX_LoadClaims_CarrierUserId ON dbo.LoadClaims(CarrierUserId);
END
GO
