nuget install -Verbosity quiet -OutputDirectory packages -ExcludeVersion Azurite -Version 1.8.3
Start-Process 'packages\Azurite\tools\blob.exe'
