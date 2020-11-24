# HacknetSharp
 Like HacknetPlusPlus but C#

It's supposed to be multiplayer hacknet

kind of

almost

but it's like

not ready to do anything right now

Purely because tarche did [HacknetPlusPlus](https://github.com/The-Council-of-Wills/HacknetPlusPlus)

## Primary end-user projects

### hsc

CLI client interface.

Supports .NET Framework 4.7.2 and .NET 5.

The core `HacknetSharp.Client` library is .NET Standard 2.0 and should be usable in most scenarios.

### hssqlite / hspostgres

Server programs, using either SQLite or Postres as backing database.

Supports .NET 5.

Additional programs are loaded from `extensions/` (must be `extensions/assemblyName/assemblyName.dll`).

## Doctor Glassman. Progress?

- [x] Some idea of wtf is going on
- [x] Client connection I/O design
- [ ] World execution loop
- [ ] Client event processing (in execution loop)
- [ ] World bootstrapping (YAML templates etc)
- [ ] Programs
- [ ] Client verbs changepassword, resetpassword