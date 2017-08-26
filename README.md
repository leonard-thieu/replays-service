# toofz Replays Service [![Build status](https://ci.appveyor.com/api/projects/status/xeoko709p63qf3jb/branch/master?svg=true)](https://ci.appveyor.com/project/leonard-thieu/replays-service/branch/master) [![codecov](https://codecov.io/gh/leonard-thieu/replays-service/branch/master/graph/badge.svg)](https://codecov.io/gh/leonard-thieu/replays-service)

## Requirements

* Windows 7 or later
* .NET Framework 4.5

## Usage

```
Usage: ReplaysService.exe [options]

options:
  --help              Shows usage information.
  --interval=VALUE    The minimum amount of time that should pass between each cycle.
  --delay=VALUE       The amount of time to wait after a cycle to perform garbage collection.
  --ikey=VALUE        An Application Insights instrumentation key.
  --replays=VALUE     The number of replays to update.
  --toofz=VALUE       The base address of toofz API.
  --username=VALUE    The user name used to log on to toofz API.
  --password[=VALUE]  The password used to log on to toofz API.
  --apikey[=VALUE]    A Steam Web API key.
  --storage[=VALUE]   An Azure Storage connection string.
```

## Contributing

### Setup

#### Additional requirements

* Visual Studio 2017
* [Azure Storage Emulator](https://go.microsoft.com/fwlink/?linkid=717179&clcid=0x409) Required to run certain tests.
* [WiX toolset](http://wixtoolset.org/releases/) Required to work with the installer.
* [toofz API](https://github.com/leonard-thieu/api.toofz.com)
* A Steam Web API key. Used to access resources from [Steam Web API](https://steamcommunity.com/dev)
  * You can obtain a Steam Web API key [by filling out this form](http://steamcommunity.com/dev/apikey).