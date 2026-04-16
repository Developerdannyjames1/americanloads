/****** Object:  Table [dbo].[States]    Script Date: 12.10.2018 20:03:08 ******/
SET ANSI_NULLS ON
GO
 
SET QUOTED_IDENTIFIER ON
GO
 
CREATE TABLE [dbo].[States](
                [Id] [int] identity(1,1) NOT NULL,
                [Code] [nvarchar](2) NULL,
                [Name] [nvarchar](50) NULL,
 CONSTRAINT [PK_States] PRIMARY KEY CLUSTERED 
(
                [Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
 
GO
 
CREATE TABLE [dbo].[OriginDestination](
                [Id] [int] identity(1,1) NOT NULL,
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
                [Id] [int] identity(1,1) NOT NULL,
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
                [Id] [int] identity(1,1) NOT NULL,
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
 
 