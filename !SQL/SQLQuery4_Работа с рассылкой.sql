
--SELECT [Language] FROM [dbo].[Users] WHERE [IsEnabled]=1 GROUP BY [Language]
--SELECT [IdEmailPush],[Name],[DateAdd],[IsCompleted] FROM [EmailPush]
--SELECT * FROM [EmailSend] WHERE IdEmail=1

--SELECT * FROM [Email] WHERE IdEmailPush=1
/*
SELECT [Users].[FirstName]+' '+[Users].[LastName] AS [UserName],[Users].[Username] AS [UserEmail],[Users].[Language] FROM [Users]
LEFT OUTER JOIN (SELECT [IdEmailPush],[UserEmail] FROM [EmailSend] WHERE [IdEmailPush]=1) AS [EmailSend] ON [EmailSend].[UserEmail]=[Users].[Username]
INNER JOIN (SELECT [Language] FROM [Email] WHERE [IdEmailPush]=1 GROUP BY [Language]) AS [UserLanguage] ON [UserLanguage].[Language]=[Users].[Language]
WHERE [Users].[IsEnabled]=1 AND [EmailSend].[IdEmailPush] IS NULL
*/
--SELECT [EmailSend].*,[Email].[Language],ISNULL([Email].[EmailSubject],NULL),([Email].[EmailBody] IS NULL) AS a FROM [EmailSend] INNER JOIN [Email] ON [Email].[IdEmail]=[EmailSend].[IdEmail]
--WHERE [EmailSend].[IdEmailPush]=1
--select null+null

--UPDATE [EmailSend] SET IsSended=0,Error=NULL,DateSend=null WHERE [IdEmailPush]=1
--UPDATE [EmailSend] SET IsSended=1,Error=NULL,DateSend=null WHERE [IdEmailPush]=1
--TRUNCATE TABLE [EmailSend]
--TRUNCATE TABLE [Email]

--SELECT [Language] FROM [Email] WHERE [IdEmailPush]=1 AND ([EmailSubject] IS NULL OR [EmailBody] IS NULL) --GROUP BY [Language]
--SELECT [Language] FROM [Email] WHERE [IdEmailPush]=1

--SELECT [Language] FROM [Users] WHERE [IsEnabled]=1 AND [Language] NOT IN (SELECT [Language] FROM [Email] WHERE [IdEmailPush]=1) GROUP BY [Users].[Language]

--SELECT [Language]+','+CONVERT(VARCHAR(4),COUNT([Language]) AS [CountLanguage] FROM [dbo].[Users] WHERE [IsEnabled]=1 GROUP BY [Language] ORDER BY [CountLanguage] DESC
--SELECT [Language]+','+CONVERT(VARCHAR(7),COUNT([Language])) AS [Language] FROM [dbo].[Users] WHERE [IsEnabled]=1 GROUP BY [Language] ORDER BY COUNT([Language]) DESC
/*
UPDATE [Email] SET IdEmailPush=3 WHERE IdEmailPush=2
UPDATE [EmailSend] SET IdEmailPush=3 WHERE IdEmailPush=2
*/

SELECT [Users].[FirstName]+' '+[Users].[LastName] AS [UserName],[Users].[Username] AS [UserEmail],[Users].[Language],[Users].[UserDateRegistration],[EmailPush].[DateUser] FROM [Users]
LEFT OUTER JOIN (SELECT [IdEmailPush],[UserEmail] FROM [EmailSend] WHERE [IdEmailPush]=2) AS [EmailSend] ON [EmailSend].[UserEmail]=[Users].[Username]
INNER JOIN (SELECT [Language] FROM [Email] WHERE [IdEmailPush]=2 GROUP BY [Language]) AS [UserLanguage] ON [UserLanguage].[Language]=[Users].[Language]
INNER JOIN (SELECT [IdEmailPush],[DateUser] FROM [EmailPush] WHERE [IdEmailPush]=2) AS [EmailPush] ON [EmailPush].[IdEmailPush]=2
WHERE [Users].[IsEnabled]=1 AND [Users].[IsSubscribe]=1 AND [EmailSend].[IdEmailPush] IS NULL AND [Users].[UserDateRegistration]>=[EmailPush].[DateUser]


select * from emailpush
select * from email
select * from emailsend