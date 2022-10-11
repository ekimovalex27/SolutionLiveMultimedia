-- =============================================
-- Author:		Alexey Ekimov
-- Create date: 12/10/2012
-- Modified date 17/10/2013
-- Modified date 26/11/2014 (ServerIp,ServerPort,ClientIp, ClientPort)
-- Description:	Logging
-- =============================================
CREATE PROCEDURE [dbo].[LogLiveMultimedia]
/* old
  @ServerIp varchar(50)=NULL,
	@ServerPort int=NULL,
  @ClientIp varchar(50)=NULL,
	@ClientPort int=NULL,
	@LogSite varchar(50),
	@LogProcedure varchar(255),
	@LogUserToken varchar(255)=NULL,
	@IdTypeLog int,
	@LogMessage nvarchar(MAX)=NULL
*/

-- new
    @ServerIp VARCHAR (50),
    @ServerPort INT,
    @ClientIp VARCHAR (50)=NULL,
    @ClientPort INT=NULL,
		@Server VARCHAR (50),
    @Scope VARCHAR (50),
		@Procedure VARCHAR (255),
		@Message NVARCHAR (MAX)=NULL,
    @UserToken VARCHAR (255)=NULL,
    @IdTypeLog INT

AS
BEGIN
	SET NOCOUNT ON;

DECLARE @Date datetime2(7)=GETDATE();

INSERT INTO LogLMM ([Date],ServerIp,ServerPort,ClientIp,ClientPort,[Server],Scope,[Procedure],[Message],UserToken,IdTypeLog)
  VALUES(@Date,@ServerIp,@ServerPort,@ClientIp,@ClientPort,@LogSite,@LogProcedure,'',@LogMessage,@LogUserToken,@IdTypeLog)

/*
INSERT INTO LogLMM ([Date],ServerIp,ServerPort,ClientIp,ClientPort,[Server],Scope,[Procedure],[Message],UserToken,IdTypeLog)
  VALUES(@Date,@ServerIp,@ServerPort,@ClientIp,@ClientPort,@Server,@Scope,@Procedure,@Message,@UserToken,@IdTypeLog)
*/

END