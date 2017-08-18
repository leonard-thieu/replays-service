# toofz Replays Service

[![Build status](https://ci.appveyor.com/api/projects/status/xeoko709p63qf3jb/branch/master?svg=true)](https://ci.appveyor.com/project/leonard-thieu/replays-service/branch/master) [![codecov](https://codecov.io/gh/leonard-thieu/replays-service/branch/master/graph/badge.svg)](https://codecov.io/gh/leonard-thieu/replays-service)

## Requirements

* Windows 7 or later
* .NET Framework 4.5

## Contributing

### Setup

#### Additional requirements

* Visual Studio 2017
* [Azure Storage Emulator](https://go.microsoft.com/fwlink/?linkid=717179&clcid=0x409)
* [toofz API](https://github.com/leonard-thieu/api.toofz.com)
* [WiX toolset](http://wixtoolset.org/releases/) is only required if you want to work with the installer.

The following environment variables and generic credentials must be set in order to run toofz Replays Service.

* Environment variables
  * `toofzApiBaseAddress` The address that toofz API is running at.
  * `ReplaysInstrumentationKey` (optional) An [Application Insights](https://azure.microsoft.com/en-us/services/application-insights/) instrumentation key. If set, toofz Replays Service will transmit telemetry.
* Generic credentials
  * `toofz/SteamWebApiKey` A Steam Web API key. Used to access resources from [Steam Web API](https://steamcommunity.com/dev)
  * `toofz/ReplaysService` User name and password for toofz API.
  * `toofz/StorageConnectionString` An [Azure Storage](https://azure.microsoft.com/en-us/services/storage/) connection string.

A script ([Setup-Environment.ps1](ReplaysService/Setup-Environment.ps1)) is included that will initialize these to development-usable values. Note, a real Steam Web API key is required. You can obtain a Steam Web API key [by filling out this form](http://steamcommunity.com/dev/apikey).

##### Syntax

`Setup-Environment.ps1 [[-ToofzApiBaseAddress] <string>] [-SteamWebApiKey] <string> [[-ToofzApiUserName] <string>] [[-ToofzApiPassword] <string>] [[-StorageConnectionString] <string>] [-Overwrite] [<CommonParameters>]`

##### Usage

`.\Setup-Environment.ps1 -SteamWebApiKey 'MySteamWebApiKey'`
