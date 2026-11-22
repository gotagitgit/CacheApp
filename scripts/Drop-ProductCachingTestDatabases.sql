USE [master];
GO

SET NOCOUNT ON;

-- Preview matching databases first. Change to 1 only when you want to drop them.
DECLARE @ExecuteDrops bit = 0;

DECLARE @MatchingDatabases table
(
    DatabaseName sysname NOT NULL PRIMARY KEY
);

INSERT INTO @MatchingDatabases (DatabaseName)
SELECT [name]
FROM sys.databases
WHERE database_id > 4
  AND [name] LIKE N'ProductCachingTest[_]%';

SELECT DatabaseName
FROM @MatchingDatabases
ORDER BY DatabaseName;

IF @ExecuteDrops = 0
BEGIN
    PRINT N'Preview only. Set @ExecuteDrops = 1 and execute again to drop the listed databases.';
    RETURN;
END;

DECLARE @DatabaseName sysname;
DECLARE @DropDatabaseSql nvarchar(max);

DECLARE DatabaseCursor CURSOR LOCAL FAST_FORWARD FOR
SELECT DatabaseName
FROM @MatchingDatabases
ORDER BY DatabaseName;

OPEN DatabaseCursor;
FETCH NEXT FROM DatabaseCursor INTO @DatabaseName;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF DB_ID(@DatabaseName) IS NOT NULL
    BEGIN
        SET @DropDatabaseSql =
            N'ALTER DATABASE ' + QUOTENAME(@DatabaseName) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE; ' +
            N'DROP DATABASE ' + QUOTENAME(@DatabaseName) + N';';

        PRINT N'Dropping ' + QUOTENAME(@DatabaseName) + N'.';
        EXEC sys.sp_executesql @DropDatabaseSql;
    END;

    FETCH NEXT FROM DatabaseCursor INTO @DatabaseName;
END;

CLOSE DatabaseCursor;
DEALLOCATE DatabaseCursor;
