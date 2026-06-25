-- One role only: "Dispatcher" (moves users from legacy "Dispatch" if present)
GO
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AspNetRoles' AND type = 'U')
BEGIN
    DECLARE @dispatchId NVARCHAR(128) = (SELECT Id FROM [dbo].[AspNetRoles] WHERE [Name] = N'Dispatch');
    DECLARE @dispatcherId NVARCHAR(128) = (SELECT Id FROM [dbo].[AspNetRoles] WHERE [Name] = N'Dispatcher');

    IF @dispatchId IS NOT NULL AND @dispatcherId IS NULL
    BEGIN
        UPDATE [dbo].[AspNetRoles] SET [Name] = N'Dispatcher' WHERE [Id] = @dispatchId;
    END
    ELSE IF @dispatchId IS NOT NULL AND @dispatcherId IS NOT NULL AND @dispatchId <> @dispatcherId
    BEGIN
        INSERT INTO [dbo].[AspNetUserRoles] ([UserId], [RoleId])
        SELECT u.[UserId], @dispatcherId
        FROM [dbo].[AspNetUserRoles] u
        WHERE u.[RoleId] = @dispatchId
          AND NOT EXISTS (
            SELECT 1 FROM [dbo].[AspNetUserRoles] x
            WHERE x.[UserId] = u.[UserId] AND x.[RoleId] = @dispatcherId
          );
        DELETE FROM [dbo].[AspNetUserRoles] WHERE [RoleId] = @dispatchId;
        DELETE FROM [dbo].[AspNetRoles] WHERE [Id] = @dispatchId;
    END
END
GO
PRINT 'MergeDispatch_Role_Into_Dispatcher: done.';
GO
