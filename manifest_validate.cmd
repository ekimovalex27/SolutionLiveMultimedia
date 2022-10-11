@ECHO OFF
SET manifest="C:\projects\SolutionLiveMultimedia\!For deploy\LiveMultimediaServer.exe.manifest"
SET manifest=LiveMultimediaServer.exe.manifest
rem SET manifest=app.manifest

"C:\Program Files (x86)\Windows Kits\8.1\bin\x64\mt.exe" -manifest %manifest% -validate_manifest
rem -check_for_duplicates
rem -validate_manifest
