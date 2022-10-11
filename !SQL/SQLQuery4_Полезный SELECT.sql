SELECT *  FROM [dbo].[Users] ORDER BY IdUser desc
SELECT *  FROM [dbo].[UserTokenClient] order by UserDateLogin desc
SELECT top 100 *  FROM [dbo].[UserTokenServer] order by IdUserTokenServer desc --IdUser desc
SELECT * FROM [dbo].[UserAccessToken] ORDER BY IdUserAccessToken DESC
SELECT * FROM [dbo].[UserRefreshToken] ORDER BY [DateRefreshToken] DESC
GO
--delete from [UserTokenServer] where IdUser=5
/*
truncate table [UserTokenServer]
truncate table [UserAccessToken]
*/
/*
truncate table [UserTokenClient]
truncate table [UserRefreshToken]
*/

/*
truncate table [UserAccessToken]
truncate table [UserRefreshToken]
*/

/*
--truncate table [Users]--
*/