# Template reference

Some examples are available under `sample/env_sample`.

## System templates

`hss new system -n <templateName>`

* Name(`string`): defines system name with replacements, e.g.
 "{Owner.UserName} Home Server" for a person with the username
 "alec" would produce "alec Home Server"
* OsName(`string`): OS name
* AddressRange(`string?`): CIDR range string for address pool
* ConnectCommandLine(`string?`): Default command to execute on shell connect
* Users(`List<string>?`): List of normal users in addition to the system's owner, formatted as "user:pass"
* Filesystem(`Dictionary<string, List<string>>?`): List of filesystem entries
  per user (can use replacement {Owner.UserName} for primary user), formatted as
  "`<type>[permissions]:<path> <args>`". Permissions are just 3 */^/+ for
  RWE, *:everyone/^:owner/+:admin can perform that operation.
  - fold: Folder.
  - prog: Program, arg[0] is the progCode of the program to execute.
  - text: Text content, arg[0] is the text to include. Unfortunately 
  it needs to be wrapped in quotes.
  - file: Content file, arg[0] determines file path. Not yet 
  implemented.
  - blob: Blob file, arg[0] determines file path. Not yet implemented.
* Vulnerabilities(`List<Vulnerability>?`): Base vulnerabilities.
  - EntryPoint(`string`): Entry point for vulnerability (port).
  - Protocol(`string`): Protocol (e.g. ssh, ftp).
  - Exploits(`int`): # exploits this vulnerability grants.
  - Cve(`string?`): Real-world CVE for fun.
* RequiredExploits(`int`): # required exploits for system access.
* RebootDuration(`double`): System reboot duration in seconds.
* DiskCapacity(`int`): System disk capacity.
* ProxyClocks(`double`): CPU cycles required to crack proxy.
* ClockSpeed(`double`): Proxy cracking speed.
* FirewallIterations(`int`): Number of firewall iterations required
  for full decode.
* FirewallLength(`int`): Length of firewall analysis string.
* FirewallDelay(`double`): Additional delay per firewall step.
* FixedFirewall(`string?`): Fixed firewall string.
* Tag(`string?`): Unique tag for lookup.

Default replacements:
* `Owner.UserName` - system owner's username
* `Owner.Name` - system owner's personal name
* `Name` / `UserName` - current filesystem section's owner name

## Person templates

`hss new person -n <templateName>`
* Username(`string?`): fixed username to use
* Password(`string?`): fixed password to use
* EmailProvider(`string?`): fixed email provider to use
* PrimaryTemplate(`string?`): fixed primary system template to use
* PrimaryAddress(`string?`): fixed primary address to use
* Usernames(`Dictionary<string,float>?`): possible usernames to be selected (weighted)
* Passwords(`Dictionary<string,float>?`): possible passwords to be selected (weighted)
* AddressRange(`string?`): CIDR range string for address pool
* EmailProviders(`Dictionary<string,float>?`): possible email providers to be 
selected (weighted)
* PrimaryTemplates(`Dictionary<string,float>?`): possible primary
  system templates to be selected (weighted)
* FleetMin(`int?`): Minimum number of additional systems
* FleetMax(`int?`): Maximum number of additional systems
* FleetTemplates(`Dictionary<string,float>?`): possible fleet system
  templates to be selected (weighted)
* Network(`List<NetworkEntry>?`): Fixed-system network to generate
  - Template(`string`): System template
  - Address(`string`): Address (combined with host network using
    subnet mask)
  - Configuration(`Dictionary<string, string>?`): Additional
    replacements to pass to system template
  - Links(`List<string>?`): Links (uni-directional) to create to
    other systems
* RebootDuration(`double`): System reboot duration in seconds.
* DiskCapacity(`int`): System disk capacity.
* ProxyClocks(`double`): CPU cycles required to crack proxy.
* ClockSpeed(`double`): Proxy cracking speed.
* Tag(`string?`): Unique tag for lookup.

## World templates

`hss new world -n <templateName>`

* PlayerSystemTemplate(`string`): template to use for players.
* PlayerAddressRange(`string?`): CIDR range string for address pool
* StartupCommandLine(`string?`): Initial command for clients to execute.
* Label(`string?`): label to use on world (just name it your template's
  name, this is only for listing the worlds with `hss world list`).
* People(`List<PersonGroup>?`): Person generators to populate world.
  - PersonTemplate(`string`): Person template to generate with.
  - Count(`int?`): Number of persons to generate with this template 
  (default 1).
  - AddressRange(`string?`): CIDR range string for address pool
* RebootDuration(`double`): System reboot duration in seconds.
* DiskCapacity(`int`): System disk capacity.

## Mission templates

`hss new mission [-n <templateName>]`

* Start(`string?`): Lua code to execute when mission starts.
* Goals(`List<string>?`): Mission goals as lua expressions that evaluate to a boolean.
* Outcomes(`List<Outcome>?`): Objective outcomes.
  - Goals(`List<int>?`): Indices of required goals (if null/empty, all goals are 
  considered).
  - Next(`string?`): Output of mission as lua code.

[Lua API reference](lua-api-reference.md)

## Server configuration

`hss new server [-n <configName>]`

* Host(`string`): external hostname to bind to.
* Port(`ushort`): external port to bind to.
* Database(`Dictionary<string, string>`): database properties.
* DefaultWorld(`string`): default world for new players to join.
* EnableLogging(`bool`): if true, enable logging.
* Motd(`string?`): Message of the day, sent to all clients during
  initial command.
* ContentFolders(`List<string>?`): Additional content directories to search.