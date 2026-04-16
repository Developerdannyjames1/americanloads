ALTER TABLE Loads ADD TrackStopId int
CREATE NONCLUSTERED INDEX IX_Loads_TrackStopId ON Loads (TrackStopId)

ALTER TABLE Loads ADD DateTSDeleted datetime