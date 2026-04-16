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
