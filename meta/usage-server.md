# Server Usage (hss)

All operations should be done inside the directory you intend to
serve content from.

## Obtaining SSL certificate

The server program requires a valid SSL certificate in order for clients to validate the server.

* Public server
  - For use on a public-facing server with a registered domain name,
use [Let's Encrypt](https://letsencrypt.org/)'s Certbot client and
obtain the required SSL certificate / private key. Locate these two
items (e.g. on Debain, `cert.pem` and `privkey.pem` under
`/etc/letsencrypt/live/<domain>/`). Combine these keys into a pfx file
with openssl.

`openssl pkcs12 -export -out cert.pfx -inkey privkey.pem -in cert.pem`

Enter some export password, it'll be reused when importing into your
keystore.

* Local server
  - For development / testing, make a self-signed root certificate for
localhost/127.0.0.1 with a request file
([example here](examplereq.cnf)).

```
openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout server.key -out server.crt -config req.cnf -extensions 'req_ext'
openssl pkcs12 -export -out cert.pfx -inkey server.key -in server.crt
```

## Installing SSL certificate

* Windows
  - Go to `Manage User Certificates` in Control Panel.
  - Click `Action > All Tasks > Import...`.
  - Select the pfx file you created.
  - Enter the export password.
  - Select "Place all certificates in the following store" and press `Browse...`
  - Select show physical stores, then select `Trusted Root Certificate Authorities > Local Computer`.
  - Finish the procedure.
* macOS
  - Double-click your .pfx and enter the export password to import it
  into your login keychain.
  - Ensure that in Get Info (Cmd+I), "Secure Sockets Layer (SSL)" is set to "Always Trust" under Trust.
* Linux
  - Use `hss cert install <pfxFile>`. After a password prompt, the certificate / private key should be installed.
  - Note: this can be reversed with `hss cert remove <certFile>` and re-entering the export password.

Ensure your certificate can be found from the domain or local address
you intend to connect to.

`hss cert search <addrOrDomain>`

## Generating templates / server configuration

Templates drive the main content. Use `hss new` to create
templates (`-e` to get an example instead of a bare file, `-n <name>`
to specify the filename).

Templates go in folders under the server directory's `templates/`
folder.

### System templates

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

Default replacements:
* `Owner.UserName` - system owner's username
* `Owner.Name` - system owner's personal name
* `Name` / `UserName` - current filesystem section's owner name

### Person templates

`hss new person -n <templateName>`

* Usernames(`List<string>`): possible usernames to be selected
* Passwords(`List<string>`): possible passwords to be selected
* AddressRange(`string?`): CIDR range string for address pool
* EmailProviders(`List<string>`): possible email providers to be 
selected
* PrimarySystemTemplates(`List<string>`): possible primary
  system templates to be selected
* FleetMin(`int?`): Minimum number of additional systems
* FleetMax(`int?`): Maximum number of additional systems
* FleetSystemTemplates(`List<string>?`): possible fleet system
  templates to be selected
* Network(`List<NetworkEntry>?`): Fixed-system network to generate
  - Template(`string`): System template
  - Address(`string`): Address (combined with host network using
    subnet mask)
  - Configuration(`Dictionary<string, string>?`): Additional
    replacements to pass to system template
  - Links(`List<string>?`): Links (uni-directional) to create to
    other systems

### World templates

`hss new world -n <templateName>`

* Label(`string`): label to use on world (just name it your template's
name, this is only for listing the worlds with `hss world list`).
* PlayerSystemTemplate(`string`): template to use for players.
* PlayerAddressRange(`string`): CIDR range string for address pool
* StartupCommandLine(`string`): Initial command for clients to execute.
* Generators(`List<Generator>?`): Person generators to populate world.
  - Count(`int`): Number of persons to generate with this template.
  - PersonTemplate(`string`): Person template to generate with.
  - AddressRange(`string?`): CIDR range string for address pool

### Server configuration

`hss new server [-n <configName>]`

* ExternalAddr(`string`): external hostname to bind to.
* DefaultWorld(`string`): default world for new players to join.

## Database operations

The program / EF Core migrations work with a database, and require a
few environment variables or server.yaml properties to be configured.

* PostgreSQL
  - hndb_kind: must be set to "postgres"
  - hndb_host: hostname for PostgreSQL server.
  - hndb_name: name of PostgreSQL database to use.
  - hndb_user: username for PostgreSQL server.
  - hndb_pass: password for PostgreSQL server.
* SQLite
  - hndb_kind: must be set to "sqlite"
  - hndb_file: Path to SQL file to use.

## Creating initial database / migrating

[Relevant Microsoft documentation](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)

You need a copy of this repository at the point at which the project
was created. [TODO verb to check latest migration OR generate 
idempotent scriptwith build]

Use the dotnet-ef tool
(`dotnet tool update -g dotnet-ef`) to generate an SQL script that
will migrate your database to the appropriate version.

`dotnet ef database update -p <folder/containing/hss.Sqlite_or_hss.Postgres_csproj> [[fromMigrationName] <toMigrationName>]`

### Setting users / content up

Set up an admin user or two. This creates an admin user (with
password prompt):

`hss user create -a <username>`

Set up a world (and remember to add it to your config as the default
world).

`hss world create <name> <templateName>`

## Running server

`hss serve`

## Command help

Just use `--help` on the program or its verbs / subverbs, they should
make sense based on their description.

(This is absolutely not laziness.)

## Extending functionality

If for whatever reason you wanted to create additional programs,
reference the `HacknetSharp.Server` project and write your
programs like the `hss.Core.CorePrograms` programs.

Your assembly (plus any dependencies not included with
`HacknetSharp.Server`) should be placed under
`extensions/assemblyName/` in your content folder. They should be
picked up by reflection during bootstrap.