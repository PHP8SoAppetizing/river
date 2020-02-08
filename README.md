Azure DevOps build:
[![Build status](https://dev.azure.com/xkit/River/_apis/build/status/River%20CI?branchName=develop)](https://dev.azure.com/xkit/River)

# River - network tunneling
River is shipped as river.exe for Windows and as a NuGet package, river.dll - for any custom .Net project.

River.dll is a .Net Standard 2.0 library for compatibility & cross platfor solutions
River.exe is a .Net Framwork 4.8 executable for Windows

# Application Usage

The commandline inspired by 'gost' project:

Run SOCKS server:
```
river -L socks://0.0.0.0:1080
```

Run ShadowSocks server:
```
river -L ss://chacha20:password@0.0.0.0:8338
```

Proxy Chain - a list of forwrders:
```
river -L socks://0.0.0.0:1080 -F socks4://rhop2:1080 -F socks4://10.7.1.1:1080 
```

# Library Usage

NuGet: https://www.nuget.org/packages/River/
Installation: Install-Package River

How to wrap you existing TCP connection to SOCKS proxy:

