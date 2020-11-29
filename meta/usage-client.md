# Client Usage (hsc)

Clients can connect to a HacknetSharp server using the hsc client.

`hsc username@server[:port]`

Don't include the port unless you know the server is running on
a non-default port.

## Registering an account

At present, registration requires either a registration token
or direct access to the host machine while the server is not running.

Once an administrator has provided a registration token, use `-r`,
which will prompt for both your desired
password and obtained registration token.

`hsc -r username@server[:port]`

## Forging tokens (admin users only)

Admin users can request registration tokens.
`hsc -f username@server[:port]`

