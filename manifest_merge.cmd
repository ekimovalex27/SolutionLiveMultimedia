

rem "C:\Program Files (x86)\Windows Kits\8.1\bin\x64\mt.exe" -inputresource:app.exe;#1 -out:extracted.manifest
rem "C:\Program Files (x86)\Windows Kits\8.1\bin\x64\mt.exe" -manifest app.manifest DeclareDPIAware.manifest -out:merged.manifest
rem "C:\Program Files (x86)\Windows Kits\8.1\bin\x64\mt.exe" -outputresource:application_name.exe;#1 -manifest merged.manifest

"C:\Program Files (x86)\Windows Kits\8.1\bin\x64\mt.exe" -manifest app.manifest DeclareDPIAware.manifest -out:merged.manifest
