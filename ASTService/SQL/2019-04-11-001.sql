CREATE TABLE [dbo].[Locations](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Location] [varchar](15) NULL,
 CONSTRAINT [PK_Locations] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

INSERT INTO [Locations] (Location) VALUES ('Corona')
INSERT INTO [Locations] (Location) VALUES ('Mexico')

ALTER TABLE AspNetUsers ADD FullName varchar(max)
ALTER TABLE AspNetUsers ADD Phone varchar(max)
ALTER TABLE AspNetUsers ADD Extension varchar(max)
ALTER TABLE AspNetUsers ADD Email2 varchar(max)
ALTER TABLE AspNetUsers ADD Location varchar(15)

ALTER TABLE Loads ADD CreateDate DateTime
ALTER TABLE Loads ADD CreatedBy nvarchar(256)
ALTER TABLE Loads ADD CreateLoc varchar(15)
ALTER TABLE Loads ADD UpdateDate DateTime
ALTER TABLE Loads ADD UpdatedBy nvarchar(256)
ALTER TABLE Loads ADD UpdateLoc varchar(15)