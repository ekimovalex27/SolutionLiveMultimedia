select top 100 IdLog,/*convert(varchar(10),[date],103) [Date]*/[Date],[IdMultimediaServer],[IdTypeLog],[ServerIp],/*[ServerPort],*/[ClientIp],[Scope],[Procedure],[Message],[UserToken] from loglmm
--where ServerIp<>'127.0.0.1'
--where Message like '%mail%'
where [Message] not like '%***%' and Message not like '%demo@***%' and Message not like '%***@mail.ru%' and Message not like '%***@outlook.com%'
order by idlog desc
--select top 1000 * from loglmm order by idlog desc
--truncate table loglmm

