# HacknetSharp
 Like HacknetPlusPlus but C#

Heavily inspired by the legendary game [Hacknet](http://hacknet-os.com/), this project aims to deliver a multiplayer Hacknet experience
to WILLs anywhere.

This project was started purely because tarche created [HacknetPlusPlus](https://github.com/The-Council-of-Wills/HacknetPlusPlus). All the WILL homies are having fun making their own
Hacknet, it's almost like Hackerjam 3 except nobody wins this time.

Server implementation hasn't been tested nor is it anywhere near
feature complete, there's even less of a warranty on this than
the standard "none." As a wise Jedi once said, "Don't try it."

## Client

`hsc` is the main terminal client. It supports .NET Framework 4.7.2
and .NET 5 ([install .NET 5 runtime here](https://dotnet.microsoft.com/download/dotnet/5.0)).

[Read about how to use the client here.](meta/usage-client.md)

Note: the core `HacknetSharp.Client` library is .NET Standard 2.0.
It *should* work in Unity under the Mono runtime.

## Server

`hssqlite` (SQLite backing database) and `hspostgres` (PostgreSQL
backing database) are the main server programs. Their only difference
is the database that supports the server. They support .NET 5
([install .NET 5 **SDK** here](https://dotnet.microsoft.com/download/dotnet/5.0)).

[Read about how to use the server here.](meta/usage-server.md)

## Doctor Glassman. Progress?

- [x] Some idea of wtf is going on
- [x] Client connection I/O design
- [x] World execution loop
- [x] Client event processing (in execution loop)
- [x] World bootstrapping (YAML templates etc)
- [ ] Execution of standard program on user's first boot (needs model support)
- [ ] Port vulnerability definitions (because this is supposed to be like Hacknet)
- [ ] Trigger user prompt after every operation (just send cwd?)
- [ ] Support for triggering AI (fake person) ProgramOperations
- [ ] Access control
- [ ] Programs
- [ ] CLIENT: lock-based terminal I/O to ensure visual consistency
- [ ] CLIENT: verbs changepassword, resetpassword
- [ ] Battle testing

##

[Hacknet](http://hacknet-os.com/)

[Steam](https://store.steampowered.com/app/365450/Hacknet) | [GoG](https://www.gog.com/game/hacknet) | [Humble Store](https://www.gog.com/game/hacknet)
