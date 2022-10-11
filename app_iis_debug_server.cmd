rem Включение и отключение отладки на стороне сервера
rem Значение True служит для разрешения отладки приложений ASP на стороне сервера. Значением по умолчанию является False.

C:\Windows\System32\inetsrv\appcmd.exe set config /section:asp /appAllowDebugging:True


