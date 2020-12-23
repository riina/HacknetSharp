# Server Usage (hss)

Note: All operations should be done inside the directory you intend to
serve content from. The folder should contain a `content` folder
and a `server.yaml` configuration file (detailed below).

[Template reference](template-reference.md)

[Mission API reference](mission-api-reference.md)

## 1. SSL certificate

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
([example here](../assets/examplereq.cnf)).

```
openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout server.key -out server.crt -config req.cnf -extensions 'req_ext'
openssl pkcs12 -export -out cert.pfx -inkey server.key -in server.crt
```

Now the certificate needs to be installed.

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

## 2. Generating templates / server configuration

Templates drive the main content. Use `hss new` to create
templates (`-e` to get an example instead of a bare file, `-n <name>`
to specify the filename). Templates go in folders under the server
directory's `content/` folder.

At a bare minimum, a properly written `server.yaml` server
configuration file, a `<?>.world.yaml` world template, and a
`<?>.system.yaml` system template for players are required.

[Template reference](template-reference.md)

See the `samples/env_sample` folder for template examples.

## 3. Creating initial database / migrating

The program / EF Core migrations work with a database, and require a
few environment variables or server.yaml properties to be configured.
(`hndb_*` are the environment variables, `Kind`/`Sqlite*`/`Postgres*`
are server.yaml properties under a `Database` root property)

* PostgreSQL
  - hndb_kind/Kind: must be set to "postgres"
  - hndb_host/PostgresHost: hostname for PostgreSQL server.
  - hndb_name/PostgresDatabase: name of PostgreSQL database to use.
  - hndb_user/PostgresUser: username for PostgreSQL server.
  - hndb_pass: password for PostgreSQL server.
* SQLite
  - hndb_kind/Kind: must be set to "sqlite"
  - hndb_file/SqliteFile: Path to SQL file to use.

### DB update option 1. EF Core tooling

[Relevant Microsoft documentation](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)

You can use EF Core's command-line tooling to apply migrations.
Making a SQL script instead of letting `hss` apply migrations by
itself is safer.

You need a copy of this repository at the point at which the project
was created.

Use the dotnet-ef tool (`dotnet tool update -g dotnet-ef`) to generate
an SQL script that will migrate your database to the appropriate
version.

`dotnet ef database update -p <folder/containing/hss.Sqlite_or_hss.Postgres_csproj> [[fromMigrationName] <toMigrationName>]`

### DB update option 2. hss database update

If you're working with Sqlite, you can backup the existing file if
applicable and just use `hss database update` to create / update the
database.

This works with Postgres, but you can't really rollback if
something goes wrong.

## 4. Setting users / content up

Set up an admin user or two with `hss user create -a <username>`

Set up a world (and remember to add it to your config as the default
world) with `hss world create <name> <templateName>`

## 5. Run server

`hss serve`

## Command help

Just use `--help` on the program or its verbs / subverbs, they should
make sense based on their description.

(This is absolutely not laziness.)

## Extending functionality

If for whatever reason you wanted to create additional programs,
reference the `HacknetSharp.Server` project and write your
programs like the `HacknetSharp.Server.CorePrograms` programs.

Your assembly (plus any dependencies not included with
`HacknetSharp.Server`) should be placed under
`extensions/assemblyName/` in your content folder. They should be
picked up by reflection during bootstrap.