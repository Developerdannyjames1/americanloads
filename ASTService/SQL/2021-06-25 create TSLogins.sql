USE [AST]
GO

/****** Object:  Table [dbo].[DATLogins]    Script Date: 25.06.2021 16:27:51 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[TSLogins](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[DateTime] [datetime] NOT NULL,
	[Message] [nvarchar](max) NULL,
	[AccessToken] [varchar](200) NULL,
	[RefreshToken] [varchar](200) NULL,
	[Expiration] [datetime] NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


alter table [dbo].[Loads] add TsLoadId uniqueidentifier
go


CREATE TABLE [dbo].[LoadsActNew](
	[Id] [int] NULL,
	[PostersReferenceId] [nvarchar](max) NULL,
	[Ltl] [bit] NULL,
	[Count] [int] NULL,
	[Stops] [int] NULL,
	[IncludeAsset] [bit] NULL,
	[PostToExtendedNetwork] [bit] NULL,
	[AvailabilityEarliest] [datetime] NULL,
	[AvailabilityLatest] [datetime] NULL,
	[AssetId] [nvarchar](max) NULL,
	[DimensionsLengthFeet] [int] NULL,
	[DimensionsWeightPounds] [int] NULL,
	[DimensionsHeightInches] [int] NULL,
	[DimensionsVolumeCubic] [int] NULL,
	[DestinationId] [int] NULL,
	[EquipmentType] [nvarchar](max) NULL,
	[OriginId] [int] NOT NULL,
	[CarrierAmount] [decimal](18, 2) NOT NULL,
	[RateEateBasedOn] [smallint] NOT NULL,
	[RateRateMiles] [int] NULL,
	[TruckStopsEnhancements] [nvarchar](max) NULL,
	[TruckStopsPosterDisplayName] [nvarchar](max) NULL,
	[ClientName] [nvarchar](max) NULL,
	[ClientLoadNum] [nvarchar](max) NULL,
	[EmailID] [nvarchar](max) NULL,
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
	[Comments] [varchar](max) NULL,
	[LoadTypeId] [int] NULL,
	[CompanyId] [int] NULL,
	[UntilDate] [datetime] NULL,
	[Description] [nvarchar](max) NULL,
	[IsLoadFull] [bit] NOT NULL,
	[PickUpDate] [datetime] NULL,
	[DeliveryDate] [datetime] NULL,
	[BanyanTechBOL] [nvarchar](15) NULL,
	[CustomerAmount] [decimal](18, 2) NULL,
	[CreateDate] [datetime] NULL,
	[CreatedBy] [nvarchar](256) NULL,
	[CreateLoc] [varchar](15) NULL,
	[UpdateDate] [datetime] NULL,
	[UpdatedBy] [nvarchar](256) NULL,
	[UpdateLoc] [varchar](15) NULL,
	[Weight] [int] NULL,
	[LengthWidthHeight] [nvarchar](max) NULL,
	[UserNotes] [varchar](max) NULL,
	[AllowUntilSat] [bit] NULL,
	[AllowUntilSun] [bit] NULL,
	[TsLoadId] [uniqueidentifier] NULL,
	[Activity] [varchar](10) null,
	[ActDate] datetime null)

go

insert LoadsActNew ([Id]
      ,[PostersReferenceId]
      ,[Ltl]
      ,[Count]
      ,[Stops]
      ,[IncludeAsset]
      ,[PostToExtendedNetwork]
      ,[AvailabilityEarliest]
      ,[AvailabilityLatest]
      ,[AssetId]
      ,[DimensionsLengthFeet]
      ,[DimensionsWeightPounds]
      ,[DimensionsHeightInches]
      ,[DimensionsVolumeCubic]
      ,[DestinationId]
      ,[EquipmentType]
      ,[OriginId]
      ,[CarrierAmount]
      ,[RateEateBasedOn]
      ,[RateRateMiles]
      ,[TruckStopsEnhancements]
      ,[TruckStopsPosterDisplayName]
      ,[ClientName]
      ,[ClientLoadNum]
      ,[EmailID]
      ,[DateLoaded]
      ,[DateRefreshed]
      ,[DateDatLoaded]
      ,[DateDatRefreshed]
      ,[DateDatDeleted]
      ,[DateRTFLoaded]
      ,[DateTRTLoaded]
      ,[AssetLength]
      ,[TrackStopId]
      ,[DateTSDeleted]
      ,[Comments]
      ,[LoadTypeId]
      ,[CompanyId]
      ,[UntilDate]
      ,[Description]
      ,[IsLoadFull]
      ,[PickUpDate]
      ,[DeliveryDate]
      ,[BanyanTechBOL]
      ,[CustomerAmount]
      ,[CreateDate]
      ,[CreatedBy]
      ,[CreateLoc]
      ,[UpdateDate]
      ,[UpdatedBy]
      ,[UpdateLoc]
      ,[Weight]
      ,[LengthWidthHeight]
      ,[UserNotes]
      ,[AllowUntilSat]
      ,[AllowUntilSun]
      ,[Activity]
      ,[ActDate])
SELECT [Id]
      ,[PostersReferenceId]
      ,[Ltl]
      ,[Count]
      ,[Stops]
      ,[IncludeAsset]
      ,[PostToExtendedNetwork]
      ,[AvailabilityEarliest]
      ,[AvailabilityLatest]
      ,[AssetId]
      ,[DimensionsLengthFeet]
      ,[DimensionsWeightPounds]
      ,[DimensionsHeightInches]
      ,[DimensionsVolumeCubic]
      ,[DestinationId]
      ,[EquipmentType]
      ,[OriginId]
      ,[CarrierAmount]
      ,[RateEateBasedOn]
      ,[RateRateMiles]
      ,[TruckStopsEnhancements]
      ,[TruckStopsPosterDisplayName]
      ,[ClientName]
      ,[ClientLoadNum]
      ,[EmailID]
      ,[DateLoaded]
      ,[DateRefreshed]
      ,[DateDatLoaded]
      ,[DateDatRefreshed]
      ,[DateDatDeleted]
      ,[DateRTFLoaded]
      ,[DateTRTLoaded]
      ,[AssetLength]
      ,[TrackStopId]
      ,[DateTSDeleted]
      ,[Comments]
      ,[LoadTypeId]
      ,[CompanyId]
      ,[UntilDate]
      ,[Description]
      ,[IsLoadFull]
      ,[PickUpDate]
      ,[DeliveryDate]
      ,[BanyanTechBOL]
      ,[CustomerAmount]
      ,[CreateDate]
      ,[CreatedBy]
      ,[CreateLoc]
      ,[UpdateDate]
      ,[UpdatedBy]
      ,[UpdateLoc]
      ,[Weight]
      ,[LengthWidthHeight]
      ,[UserNotes]
      ,[AllowUntilSat]
      ,[AllowUntilSun]
      ,[Activity]
      ,[ActDate]
  FROM [AST].[dbo].[LoadsAct]
go 

drop table [LoadsAct]
go 

sp_rename 'LoadsActNew','LoadsAct'
go


alter table [dbo].[LoadTypes] add TsId int null
go

update [LoadTypes] set TsId = 3 where IdTS = 'AUTO'
update [LoadTypes] set TsId = 8 where IdTS = 'CONT'
update [LoadTypes] set TsId = 9 where IdTS = 'DD'
update [LoadTypes] set TsId = 10 where IdTS = 'DUMP'
update [LoadTypes] set TsId = 12 where IdTS = 'F'
update [LoadTypes] set TsId = 59 where IdTS = 'FA'
update [LoadTypes] set TsId = 20 where IdTS = 'HS'
update [LoadTypes] set TsId = 18 where IdTS = 'FWS'
update [LoadTypes] set TsId = 16 where IdTS = 'FSD'
update [LoadTypes] set TsId = 61 where IdTS = 'FRV'
update [LoadTypes] set TsId = 19 where IdTS = 'HOPP'
update [LoadTypes] set TsId = 23 where IdTS = 'LB'
update [LoadTypes] set TsId = 52 where IdTS = 'VM'
update [LoadTypes] set TsId = 29 where IdTS = 'PNEU'
update [LoadTypes] set TsId = 30 where IdTS = 'PO'
update [LoadTypes] set TsId = 31 where IdTS = 'R'
update [LoadTypes] set TsId = 34 where IdTS = 'RINT'
update [LoadTypes] set TsId = 66 where IdTS = 'SDRG'
update [LoadTypes] set TsId = 37 where IdTS = 'SD'
update [LoadTypes] set TsId = 13 where IdTS = 'FEXT'
update [LoadTypes] set TsId = 43 where IdTS = 'V'
update [LoadTypes] set TsId = 58 where IdTS = 'VA'
update [LoadTypes] set TsId = 46 where IdTS = 'CV'
update [LoadTypes] set TsId = 50 where IdTS = 'VIV'
update [LoadTypes] set TsId = 49 where IdTS = 'VINT'
update [LoadTypes] set TsId = 51 where IdTS = 'VLG'
update [LoadTypes] set TsId = 44 where IdTS = 'V-OT'
update [LoadTypes] set TsId = 54 where IdTS = 'VV'
update [LoadTypes] set TsId = 60 where IdTS = 'FV'
update [LoadTypes] set TsId = 45 where IdTS = 'VB'
update [LoadTypes] set TsId = 5 where IdTS = 'BELT'
update [LoadTypes] set TsId = 15 where IdTS = 'FO'
update [LoadTypes] set TsId = 24 where IdTS = 'LBO'
update [LoadTypes] set TsId = 78 where IdTS = 'CONG'
update [LoadTypes] set TsId = 74 where IdTS = 'DA'
go