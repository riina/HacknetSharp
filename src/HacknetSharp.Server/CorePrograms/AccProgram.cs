using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:acc", "acc", "manage local accounts",
        "add, list, or delete local accounts\n\n" +
        "add [-a] <account>: [ADMIN ONLY] add a new standard\n" +
        "account (password will be prompted)\n" +
        "\t-a: Create admin account (requires login to be owner)\n\n" +
        "list: list all accounts\n\n" +
        "delete <account>: [ADMIN ONLY] delete an account",
        "[-adl] [args]", true)]
    public class AccProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            var logins = System.Logins;
            var spawn = World.Spawn;
            var (flags, _, args) = IsolateArgvFlags(Argv);
            if (args.Count == 0)
            {
                Write("Verb not specified, must be add, list, or delete\n");
                yield break;
            }

            switch (args[0].ToLowerInvariant())
            {
                case "add":
                    {
                        if (!Login.Admin)
                        {
                            Write("Permission denied\n");
                            break;
                        }

                        if (args.Count != 2)
                        {
                            Write("Invalid number of arguments, must be <account>\n");
                            break;
                        }

                        string name = args[1];
                        if (logins.Any(l => l.User == name))
                        {
                            Write("An account with the specified name already exists\n");
                            break;
                        }

                        bool admin = flags.Contains("a");
                        if (admin && Login.Person != System.Owner.Key)
                        {
                            Write("Only the system owner may create admin accounts\n");
                            break;
                        }

                        Write("Password:");
                        var input = Input(User, true);
                        yield return input;
                        var (hash, salt) = ServerUtil.HashPassword(input.Input!.Input);
                        spawn.Login(System, name, hash, salt, admin);
                        break;
                    }
                case "delete":
                    {
                        if (!Login.Admin)
                        {
                            Write("Permission denied\n");
                            break;
                        }

                        if (args.Count != 2)
                        {
                            Write("Invalid number of arguments, must be <account>\n");
                            break;
                        }

                        string name = args[1];
                        var toDelete = logins.FirstOrDefault(l => l.User == name);
                        if (toDelete == null)
                        {
                            Write("The specified account does not exist\n");
                            break;
                        }

                        if (toDelete.Admin && Login.Person != System.Owner.Key)
                        {
                            Write("Only the system owner may delete admin accounts\n");
                            break;
                        }

                        Write($"Are you sure you want to delete account {toDelete.User}?\n");
                        var confirm = Confirm(false);
                        yield return confirm;
                        if (!confirm.Confirmed) yield break;

                        spawn.RemoveLogin(toDelete);
                        break;
                    }
                case "list":
                    {
                        var sb = new StringBuilder();
                        foreach (var l in logins) sb.Append(IC, $"{l.User} ({(l.Admin ? "admin" : "standard")})\n");
                        if (sb.Length == 0) sb.Append('\n');
                        Write(sb.ToString());
                        break;
                    }
                default:
                    {
                        Write("Invalid verb, must be add, list, or delete\n");
                        break;
                    }
            }
        }
    }
}
