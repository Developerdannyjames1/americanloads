CREATE TABLE [dbo].[Loads](
                [Id] [int] identity(1,1) NOT NULL,
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
				[ClientName] nvarchar(max) null,
				[ClientLoadNum] varchar(30) null,
				[EmailID] varchar(100) null,
				[DateLoaded] datetime null, 
				[DateRefreshed] datetime null, 
				[DateDatLoaded] datetime null, 
				[DateDatRefreshed] datetime null, 
				[DateDatDeleted] datetime null,
				[DateRTFLoaded] datetime null, 
				[DateTRTLoaded] datetime null, 
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
 