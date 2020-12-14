# HacknetSharp
 multiplayer hacknet-like game-like thing

[![.NET Build](https://github.com/The-Council-of-Wills/HacknetSharp/workflows/.NET%20Build/badge.svg)](https://github.com/The-Council-of-Wills/HacknetSharp/actions?workflow=.NET+Build)

| Package                | Release |
|------------------------|---------|
| `Fp`           | [![NuGet](https://img.shields.io/nuget/v/HacknetSharp.svg)](https://www.nuget.org/packages/HacknetSharp/)|
| `Fp.Templates` | [![NuGet](https://img.shields.io/nuget/v/HacknetSharp.Server.svg)](https://www.nuget.org/packages/HacknetSharp.Server/) |

[API Reference](https://the-council-of-wills.github.io/HacknetSharp/api/index.html)

## Current Status

Most significant programs are implemented:
* Filesystem interaction (cd/cat/edit/mv/cp/rm)
* Environment variables (set)
* Process management (ps/kill)
* Network (ssh/exit/scan/map)
* Port hacking (probe/`<one-size-fits-all core:hack>`/porthack)
* Utility (echo/help)

There are still some features missing, listed on the
[MVP project board](https://github.com/The-Council-of-Wills/HacknetSharp/projects/1).

## Client
[Usage](meta/usage-client.md)

`hsh` is the main terminal client. It supports .NET 5
([install .NET 5 runtime here](https://dotnet.microsoft.com/download/dotnet/5.0)).

## Server
[Usage](meta/usage-server.md)

`hss` is the main server program. It supports .NET 5
([install .NET 5 **SDK** here](https://dotnet.microsoft.com/download/dotnet/5.0)).

##

[Hacknet](http://hacknet-os.com/)

[Steam](https://store.steampowered.com/app/365450/Hacknet) | [GoG](https://www.gog.com/game/hacknet) | [Humble Store](https://www.gog.com/game/hacknet)
