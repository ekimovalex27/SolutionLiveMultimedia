select top 100 * from MultimediaFile where iduser not in (1,2,5,91) order by idmultimediafile desc

--select count(*) from MultimediaFile where iduser=1
--select count(*) from MultimediaFile where iduser=2
--select count(*) from MultimediaFile where iduser=5

--delete from MultimediaFile where iduser=5
--select * from MultimediaFile where iduser=5 order by FullPath
--delete from MultimediaFile where iduser<>2
--select * from MultimediaFile where iduser=5 and FullPath='\\DLINK-8C754C\Multimedia\music\Русские\Кино\Кино (для записи)\Sasha.mp3'
--select * from MultimediaFile where iduser=5 and FullPath like '%\\DLINK-8C754C\Multimedia\music\Классика\13-Chopin. Waltz in F minor, Op70 No2.mp3%'
--select * from MultimediaFile where iduser=5 and FullPath like '%\\DLINK-8C754C\Multimedia\music\Классика%' order by FullPath
--select count(*) from MultimediaFile
--select * from MultimediaFile order by IdMultimediaFile desc

/*
declare @s1 nvarchar(max)
declare @s2 nvarchar(max)
set @s1='\\DLINK-8C754C\Multimedia\music\Аудиокниги\Аэлита\gl00-01.mp3'
set @s2='\\DLINK-8C754C\Multimedia\music\Аудиокниги\Аэлита\gl00-01.mp3'
select (@s1=@s2)
*/