# toofz Replays Service

[![Build status](https://ci.appveyor.com/api/projects/status/xeoko709p63qf3jb/branch/master?svg=true)](https://ci.appveyor.com/project/leonard-thieu/replays-service/branch/master)
[![codecov](https://codecov.io/gh/leonard-thieu/replays-service/branch/master/graph/badge.svg)](https://codecov.io/gh/leonard-thieu/replays-service)

## Overview

**toofz Replays Service** is a backend service that handles updating [Crypt of the NecroDancer](http://necrodancer.com/) replays for [toofz API](https://api.toofz.com/). 
It polls [Steam Web API](https://partner.steamgames.com/doc/webapi_overview) at regular intervals to provide up-to-date data.

---

**toofz Replays Service** is a component of **toofz**. 
Information about other projects that support **toofz** can be found in the [meta-repository](https://github.com/leonard-thieu/toofz-necrodancer).

### Dependents

* [toofz API](https://github.com/leonard-thieu/api.toofz.com)

### Dependencies

* [toofz NecroDancer Core](https://github.com/leonard-thieu/toofz-necrodancer-core)
* [toofz Steam](https://github.com/leonard-thieu/toofz-steam)
* [toofz Data](https://github.com/leonard-thieu/toofz-data)
* [toofz Services Core](https://github.com/leonard-thieu/toofz-services-core)

## Requirements

* .NET Framework 4.6.1
* MS SQL Server

## License

**toofz Replays Service** is released under the [MIT License](LICENSE).