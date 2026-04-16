ALTER TABLE Loads ADD IsLoadFull BIT NOT NULL DEFAULT(0)
ALTER TABLE Loads ADD PickUpDate DateTime
ALTER TABLE Loads ADD DeliveryDate DateTime

ALTER TABLE DATLogins ADD TokenPrimary binary(100)
ALTER TABLE DATLogins ADD TokenSecondary binary(100)
ALTER TABLE DATLogins ADD Expiration DateTime
