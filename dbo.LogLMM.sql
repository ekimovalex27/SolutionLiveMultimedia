CREATE TABLE [dbo].[LogLMM] (
    [IdLog]        BIGINT         IDENTITY (1, 1) NOT NULL,
    [Date]      DATETIME2 (7)  NOT NULL,
    [ServerIp]     VARCHAR (50)   NULL,
    [ServerPort]   INT            NULL,
    [ClientIp]     VARCHAR (50)   NULL,
    [ClientPort]   INT            NULL,
		[Server]      VARCHAR (50)   NULL,
    [Scope]      VARCHAR (50)   NULL,
		[Procedure] VARCHAR (255)  NULL,
		[Message]   NVARCHAR (MAX) NULL,
    [UserToken] VARCHAR (255)  NULL,
    [IdTypeLog]    INT            NOT NULL,    
    CONSTRAINT [PK_LogLMM] PRIMARY KEY CLUSTERED ([IdLog] ASC)
);

