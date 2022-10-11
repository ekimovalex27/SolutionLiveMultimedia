rem Включение и отключение отладки на стороне клиента
rem Значение True служит для включения отладки на стороне клиента. Значением по умолчанию является False.

C:\Windows\System32\inetsrv\appcmd.exe set config /section:asp /appAllowClientDebug:True
