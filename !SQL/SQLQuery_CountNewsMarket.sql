SELECT[Id]=[ned],[Name]=CASE([CountryNative])WHEN [CountryEnglish] THEN [CountryNative]ELSE [CountryNative]+'('+[CountryEnglish]+')'END,0 AS [IdTypeMultimediaItem]FROM [NewsMarketGoogle] ORDER BY [CountryEnglish]

SELECT [Id]=[ned],[Name]=
CASE ([CountryNative])
WHEN [CountryEnglish] THEN [CountryNative]
ELSE [CountryNative]+'('+[CountryEnglish]+')'
END
,
0 AS [IdTypeMultimediaItem]
FROM [NewsMarketGoogle] ORDER BY [CountryEnglish]

SELECT [Id]=[MarketId],[Name]=
CASE (SELECT COUNT(*) FROM [NewsMarket] AS [CountNewsMarket] WHERE [CountNewsMarket].[Country]=[NewsMarket].[Country])
WHEN 1 THEN [Country]
ELSE [Country]+'|'+[Language]
END,
0 AS [IdTypeMultimediaItem]
FROM [NewsMarket] ORDER BY [Country]

