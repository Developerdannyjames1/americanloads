SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[LoadTypes](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[IdDAT] [nvarchar](200) NOT NULL,
	[NameDAT] [nvarchar](200) NOT NULL,
	[IdTS] [nvarchar](200) NOT NULL,
	[NameTS] [nvarchar](200) NOT NULL,
 CONSTRAINT [PK_LoadTypes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Auto Carrier', 'AutoCarrier', 'AUTO', 'Auto')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Container', 'Container', 'CONT', 'Container Trailer')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Double Drop', 'DoubleDrop', 'DD', 'Double Drop')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Dump Trailer', 'DumpTrailer', 'DUMP', 'Dump Trucks')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Flatbed', 'Flatbed', 'F', 'Flatbed')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Flatbed, Air-Ride', 'FlatbedAirRide', 'FA', 'Flatbed Air-Ride')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Flatbed, Hotshot', 'FlatbedHotshot', 'HS', 'Hot Shot')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Flatbed w/Sides', 'FlatbedwSides', 'FWS', 'Flatbed With Sides')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Flatbed or Step Deck', 'FlatbedorStepDeck', 'FSD', 'Flat or Step Deck')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Flatbed/Van/Reefer', 'FlatbedVanReefer', 'FRV', 'Flatbed, Van, or Reefer')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Hopper Bottom', 'HopperBottom', 'HOPP', 'Hopper Bottom')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Lowboy', 'Lowboy', 'LB', 'Lowboy')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Moving Van', 'MovingVan', 'VM', 'Moving Van')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Pneumatic', 'Pneumatic', 'PNEU', 'Pneumatic')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Power Only', 'PowerOnly', 'PO', 'Power Only (tow-away)')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Reefer', 'Reefer', 'R', 'Refrigerated Carrier(Reefer)')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Reefer, Intermodal', 'ReeferIntermodal', 'RINT', 'Refrigerated Intermodal')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Removable Gooseneck', 'RemovableGooseneck', 'SDRG', 'Step Deck or Removable Gooseneck')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Step Deck', 'StepDeck', 'SD', 'Step Deck')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Stretch Trailer', 'StretchTrailer', 'FEXT', 'Stretch Trailers or Extendable Flatbed')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Van', 'Van', 'V', 'Van')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Van, Air-Ride', 'VanAirRide', 'VA', 'Van Air-Ride')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Van, Curtain', 'VanCurtain', 'CV', 'Curtain Van')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Van, Insulated', 'VanInsulated', 'VIV', 'Vented Insulated Van')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Van, Intermodal', 'VanIntermodal', 'VINT', 'Van Intermodal')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Van, Lift-Gate', 'VanLiftGate', 'VLG', 'Van w/Liftgate')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Van, Open-Top', 'VanOpenTop', 'V-OT', 'Open Top Van')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Van, Vented', 'VanVented', 'VV', 'Vented Van')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Van or Flatbed', 'VanorFlatbed', 'FV', 'Van or Flatbed')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Van, w/Blanket Wrap', 'VanwBlanketWrap', 'VB', 'Blanket Wrap Van')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Conveyor', 'Conveyor', 'BELT', 'Conveyor Belt')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Flatbed, Over Dimension', 'FlatbedOverDimension', 'FO', 'Flatbed over-dimension loads')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Lowboy, Over Dimension', 'LowboyOverDimension', 'LBO', 'Lowboy over-dimension load')
INSERT INTO LoadTypes(Name, NameDAT, IdDAT, IdTS, NameTS) VALUES ('-', 'Conestoga', 'Conestoga', 'CONG', 'Conestoga')
UPDATE LoadTypes SET Name = NameDAT

ALTER TABLE Loads ADD LoadTypeId int
GO

ALTER TABLE [dbo].[Loads]  WITH CHECK ADD  CONSTRAINT [FK_Loads_LoadTypes] FOREIGN KEY([LoadTypeId])
REFERENCES [dbo].[LoadTypes] ([Id])
GO

ALTER TABLE [dbo].[Loads] CHECK CONSTRAINT [FK_Loads_LoadTypes]
GO


CREATE TABLE [dbo].[Companies](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NULL,
 CONSTRAINT [PK_Companies] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

INSERT INTO Companies (Name) VALUES ('United Rentals')
GO

ALTER TABLE Loads ADD CompanyId int
GO

ALTER TABLE [dbo].[Loads]  WITH CHECK ADD  CONSTRAINT [FK_Loads_Companies] FOREIGN KEY([CompanyId])
REFERENCES [dbo].[Companies] ([Id])
GO

ALTER TABLE [dbo].[Loads] CHECK CONSTRAINT [FK_Loads_Companies]
GO
