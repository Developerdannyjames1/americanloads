CREATE TABLE [dbo].[ZIPCodes](
	[ZipCode] [char](5) NOT NULL,
	[City] [varchar](35) NULL,
	[State] [char](2) NULL,
	[Latitude] [decimal](12, 4) NULL,
	[Longitude] [decimal](12, 4) NULL,
	[Classification] [varchar](1) NULL,
	[Population] [int] NULL
) ON [PRIMARY]
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'00000-99999 Five digit numeric ZIP Code of the area.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ZIPCodes', @level2type=N'COLUMN',@level2name=N'ZipCode'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'2 letter state name abbreviation.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ZIPCodes', @level2type=N'COLUMN',@level2name=N'State'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Geographic coordinate as a point measured in degrees north or south of the equator.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ZIPCodes', @level2type=N'COLUMN',@level2name=N'Latitude'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Geographic coordinate as a point measured in degrees east or west of the Greenwich Meridian.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ZIPCodes', @level2type=N'COLUMN',@level2name=N'Longitude'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'U.S. Zip Code Database Free (from www.zip-codes.com)' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'ZIPCodes'
GO



CREATE TABLE [dbo].[States](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Code] [nvarchar](2) NULL,
	[Name] [nvarchar](50) NULL,
 CONSTRAINT [PK_States] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO



CREATE TABLE [dbo].[OriginDestination](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Type] [smallint] NOT NULL,
	[City] [nvarchar](50) NULL,
	[County] [nvarchar](50) NULL,
	[StateId] [int] NULL,
	[PostalCode] [nvarchar](50) NULL,
	[Country] [nvarchar](2) NULL,
	[Latitude] [decimal](18, 5) NULL,
	[Longitude] [decimal](18, 5) NULL,
 CONSTRAINT [PK_OriginDestination] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[OriginDestination]  WITH CHECK ADD  CONSTRAINT [FK_OriginDestination_States] FOREIGN KEY([StateId])
REFERENCES [dbo].[States] ([Id])
GO

ALTER TABLE [dbo].[OriginDestination] CHECK CONSTRAINT [FK_OriginDestination_States]
GO



CREATE TABLE [dbo].[Assets](
	[PostersReferenceId] [nvarchar](max) NULL,
	[Ltl] [bit] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Count] [int] NULL,
	[Stops] [int] NULL,
	[IncludeAsset] [bit] NULL,
	[PostToExtendedNetwork] [bit] NULL,
	[AvailabilityEarliest] [datetime] NULL,
	[AvailabilityLatest] [datetime] NULL,
	[AssetId] [nvarchar](15) NULL,
	[DimensionsLengthFeet] [int] NULL,
	[DimensionsWeightPounds] [int] NULL,
	[DimensionsHeightInches] [int] NULL,
	[DimensionsVolumeCubic] [int] NULL,
	[DestinationId] [int] NOT NULL,
	[EquipmentType] [nvarchar](20) NULL,
	[OriginId] [int] NOT NULL,
	[RateBaseRateDollars] [decimal](18, 2) NOT NULL,
	[RateEateBasedOn] [smallint] NOT NULL,
	[RateRateMiles] [int] NULL,
	[TruckStopsEnhancements] [nvarchar](50) NULL,
	[TruckStopsPosterDisplayName] [nvarchar](20) NULL,
 CONSTRAINT [PK_Assets] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[Assets]  WITH CHECK ADD  CONSTRAINT [FK_Assets_Destination] FOREIGN KEY([DestinationId])
REFERENCES [dbo].[OriginDestination] ([Id])
GO

ALTER TABLE [dbo].[Assets] CHECK CONSTRAINT [FK_Assets_Destination]
GO

ALTER TABLE [dbo].[Assets]  WITH CHECK ADD  CONSTRAINT [FK_Assets_Origin] FOREIGN KEY([OriginId])
REFERENCES [dbo].[OriginDestination] ([Id])
GO

ALTER TABLE [dbo].[Assets] CHECK CONSTRAINT [FK_Assets_Origin]
GO

CREATE TABLE [dbo].[AssetComments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AssetId] [int] NOT NULL,
	[Comment] [nvarchar](max) NULL,
 CONSTRAINT [PK_AssetComments] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[AssetComments]  WITH CHECK ADD  CONSTRAINT [FK_AssetComments_Assets] FOREIGN KEY([AssetId])
REFERENCES [dbo].[Assets] ([Id])
GO

ALTER TABLE [dbo].[AssetComments] CHECK CONSTRAINT [FK_AssetComments_Assets]
GO


CREATE TABLE [dbo].[Loads](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[PostersReferenceId] [nvarchar](max) NULL,
	[Ltl] [bit] NULL,
	[Count] [int] NULL,
	[Stops] [int] NULL,
	[IncludeAsset] [bit] NULL,
	[PostToExtendedNetwork] [bit] NULL,
	[AvailabilityEarliest] [datetime] NULL,
	[AvailabilityLatest] [datetime] NULL,
	[AssetId] [nvarchar](15) NULL,
	[DimensionsLengthFeet] [int] NULL,
	[DimensionsWeightPounds] [int] NULL,
	[DimensionsHeightInches] [int] NULL,
	[DimensionsVolumeCubic] [int] NULL,
	[DestinationId] [int] NOT NULL,
	[EquipmentType] [nvarchar](20) NULL,
	[OriginId] [int] NOT NULL,
	[RateBaseRateDollars] [decimal](18, 2) NOT NULL,
	[RateEateBasedOn] [smallint] NOT NULL,
	[RateRateMiles] [int] NULL,
	[TruckStopsEnhancements] [nvarchar](50) NULL,
	[TruckStopsPosterDisplayName] [nvarchar](20) NULL,
	[ClientName] [nvarchar](max) NULL,
	[ClientLoadNum] [varchar](30) NULL,
	[EmailID] [varchar](100) NULL,
	[DateLoaded] [datetime] NULL,
	[DateRefreshed] [datetime] NULL,
	[DateDatLoaded] [datetime] NULL,
	[DateDatRefreshed] [datetime] NULL,
	[DateDatDeleted] [datetime] NULL,
	[DateRTFLoaded] [datetime] NULL,
	[DateTRTLoaded] [datetime] NULL,
	[AssetLength] [int] NULL,
	[TrackStopId] [int] NULL,
	[DateTSDeleted] [datetime] NULL,
 CONSTRAINT [PK_Loads] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[Loads]  WITH CHECK ADD  CONSTRAINT [FK_Loads_Destination] FOREIGN KEY([DestinationId])
REFERENCES [dbo].[OriginDestination] ([Id])
GO

ALTER TABLE [dbo].[Loads] CHECK CONSTRAINT [FK_Loads_Destination]
GO

ALTER TABLE [dbo].[Loads]  WITH CHECK ADD  CONSTRAINT [FK_Loads_Origin] FOREIGN KEY([OriginId])
REFERENCES [dbo].[OriginDestination] ([Id])
GO

ALTER TABLE [dbo].[Loads] CHECK CONSTRAINT [FK_Loads_Origin]
GO

CREATE TABLE [dbo].[LoadComments](
	[Id] [int] NOT NULL,
	[LoadId] [int] NOT NULL,
	[Comment] [nvarchar](max) NULL,
 CONSTRAINT [PK_LoadComments] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[LoadComments]  WITH CHECK ADD  CONSTRAINT [FK_LoadComments_Loads] FOREIGN KEY([LoadId])
REFERENCES [dbo].[Loads] ([Id])
GO

ALTER TABLE [dbo].[LoadComments] CHECK CONSTRAINT [FK_LoadComments_Loads]
GO



CREATE TABLE [dbo].[UploadLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Begin] [datetime] NOT NULL,
	[Company] [varchar](100) NULL,
	[LoadNo] [varchar](50) NULL,
	[Converted] [bit] NULL,
	[ConvertError] [varchar](max) NULL,
	[Uploaded] [bit] NULL,
	[UploadError] [varchar](max) NULL,
	[Finished] [datetime] NULL,
	[MessageID] varchar(100) null,
	[FileName] varchar(max) null,
	[SendAttempts] int null,
 CONSTRAINT [PK_UploadLog] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

ALTER TABLE [dbo].[UploadLog] ADD  CONSTRAINT [DF_ASTServiceLog_Begin]  DEFAULT (getdate()) FOR [Begin]
GO


