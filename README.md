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

CLI client interface. (The core `HacknetSharp.Client` library is
.NET Standard 2.0 and should be usable in most scenarios).

### hss

Standard server implementation.

Adding programs is done simply by adding assemblies in folders named 
according to the assembly file names under `extensions/`.

Using programs with extra database types would require a copy of this 
project's source and an ef migration.

## Doctor Glassman. Progress?

- [x] Some idea of wtf is going on
- [ ] Client verbs connect, register, changepassword
- [ ] Server verbs adminregister, maketoken
- [ ] World bootstrapping (YAML templates etc)
- [ ] Client connection I/O design
- [ ] Programs