[![.NET Build](https://github.com/The-Council-of-Wills/HacknetSharp/workflows/.NET%20Build/badge.svg)](https://github.com/The-Council-of-Wills/HacknetSharp/actions?workflow=.NET+Build)
[![Nuget](https://img.shields.io/nuget/v/HacknetSharp?label=HacknetSharp&logo=Nuget)](https://www.nuget.org/packages/HacknetSharp/)
[![Nuget](https://img.shields.io/nuget/v/HacknetSharp.Server?label=HacknetSharp.Server&logo=Nuget)](https://www.nuget.org/packages/HacknetSharp.Server/)

# Multiplayer hacknet-like game-like thing

[Documentation](https://the-council-of-wills.github.io/HacknetSharp/articles/intro.html)

[API Reference](https://the-council-of-wills.github.io/HacknetSharp/api/index.html)

## Current Status

Most significant programs are implemented:
* Filesystem interaction (cd/cat/edit/mv/cp/rm)
* Environment variables (set)
* Process management (ps/kill)
* Network (ssh/exit/scan/map)
* Hacking
  - Ports (`<one-size-fits-all core:hack>`)
  - Firewall (analyze/solve)
  - General (probe/porthack)
  - Proxy (shell/overload/trap)
* Utility (echo/help/login/acc)
* Chatting (chatd/chat/c)

There are still some features missing, listed on the
[MVP project board](https://github.com/The-Council-of-Wills/HacknetSharp/projects/1).

## Client
[Usage](docfx_project/articles/usage-client.md)

`hsh` is the main terminal client. It supports .NET 5
([install .NET 5 runtime here](https://dotnet.microsoft.com/download/dotnet/5.0)).

## Server
[Usage](docfx_project/articles/usage-server.md)

`hss` is the main server program. It supports .NET 5
([install .NET 5 **SDK** here](https://dotnet.microsoft.com/download/dotnet/5.0)).

##

[Hacknet](http://hacknet-os.com/)

[Steam](https://store.steampowered.com/app/365450/Hacknet) | [GoG](https://www.gog.com/game/hacknet) | [Humble Store](https://www.gog.com/game/hacknet)
