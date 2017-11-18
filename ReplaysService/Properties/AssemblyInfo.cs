﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using log4net.Config;

[assembly: AssemblyVersion("3.3.0.0")]

[assembly: AssemblyCopyright("Copyright © Leonard Thieu 2017")]
[assembly: AssemblyProduct("toofz")]

[assembly: AssemblyTitle("toofz Replays Service")]

[assembly: ComVisible(false)]

[assembly: InternalsVisibleTo("ReplaysService.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

[assembly: XmlConfigurator(Watch = true, ConfigFile = "log.config")]
