--select count(*) from localization
--select * from localization
--select top 100 * from localization where [Project]='LiveMultimediaSystem' order by [DateAdd] desc

--delete from localization where [Project]='LiveMultimediaSystem' AND [Topic]='NewsMarket'
--delete from localization where [IdLocalization] IN (8372)
--select * from [Localization] where [Project]='LiveMultimediaSystem' AND [Topic] LIKE 'News%' order by [DateAdd] desc
--select * from [Localization] where [Project]='LiveMultimediaSystem' AND [Topic]='Local' AND [Language]='en' order by [DateAdd] desc
--select top 100 * from [Localization] where [Project]='LiveMultimediaSystem' order by [DateAdd] desc

--select top 100 * from localization /*where [project]='LiveMultimediaSystem' AND [Topic]='Remote'*/ order by [DateAdd] desc

--select * from localization where Language<>'en'
--select * from localization where Language<>'en' and Topic='Remote'
--select * from localization where Language<>'en' and Topic='Local'

--truncate table localization

--delete from localization where Language<>'en' and Topic='Remote'
--delete from localization where Language<>'en' and Topic='Local'
--delete from localization where Language<>'en'

--select * from [Localization] where [Language]<>'en' and Project='jetsas.com' order by IdLocalization desc

--select * from [Localization] where [Language]='ru' and Project='jetsas.com' order by IdLocalization desc
--select * from [Localization] where [Language]='en' and Project='jetsas.com' order by IdLocalization desc
--delete from [Localization] where [Language]<>'en' and Project='jetsas.com'

--select * from localization where Language='en' and Topic='Remote' order by ElementName
--select * from localization where Language='en' and Topic='Local'
--select * from localization where Language='en' and Project='jetsas.com'

--update localization set Project='LiveMultimediaSystem'
--update localization set Project='jetsas.com' where Project='www.jetsas.com'

--select * from localization where Language='en' and Project='LiveMultimediaSystem' AND Topic='Remote' AND ElementName like '%Default_%'
--select * from localization where Language='fi' and Project='LiveMultimediaSystem' AND Topic='Service' --AND ElementName like '%Default_%'

--select * from localization where ElementName like '%MasterPage_Username_ToolTip%'

-- tlh-Qaak
--delete from localization where Language='tlh-Qaak'
--select * from localization where Language='tlh-Qaak'


--INSERT INTO [Localization] ([AccountKey],[Project],[Topic],[ElementName],[ElementValue],[IsDefaultLanguage],[Language])
--VALUES ('k9mF29BQBMGAhhua461N7SaIe0SzmtGyQC33zmqZU44','jetsas.com','Default','Application1_Note','Helps you plan your departure',1,'en')

select * from [Localization] where topic='NewsMarket' order by IdLocalization desc
--delete from localization where topic='NewsMarket'
