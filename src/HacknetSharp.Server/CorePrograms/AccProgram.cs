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
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var system = context.System;
            var logins = system.Logins;
            var login = context.Login;
            var spawn = context.World.Spawn;
            var (flags, _, args) = IsolateArgvFlags(context.Argv);
            if (args.Count == 0)
            {
                user.WriteEventSafe(Output("Verb not specified, must be add, list, or delete\n"));
                user.FlushSafeAsync();
                yield break;
            }

            switch (args[0].ToLowerInvariant())
            {
                case "add":
                {
                    if (!login.Admin)
                    {
                        user.WriteEventSafe(Output("Permission denied\n"));
                        user.FlushSafeAsync();
                        break;
                    }

                    if (args.Count != 2)
                    {
                        user.WriteEventSafe(Output("Invalid number of arguments, must be <account>\n"));
                        user.FlushSafeAsync();
                        break;
                    }

                    string name = args[1];
                    if (logins.Any(l => l.User == name))
                    {
                        user.WriteEventSafe(Output("An account with the specified name already exists\n"));
                        user.FlushSafeAsync();
                        break;
                    }

                    bool admin = flags.Contains("a");
                    if (admin && login.Person != system.Owner.Key)
                    {
                        user.WriteEventSafe(Output("Only the system owner may create admin accounts\n"));
                        user.FlushSafeAsync();
                        break;
                    }

                    user.WriteEventSafe(Output("Password:"));
                    var input = Input(user, true);
                    yield return input;
                    var (hash, salt) = ServerUtil.HashPassword(input.Input!.Input);
                    spawn.Login(context.System, name, hash, salt, admin);
                    break;
                }
                case "delete":
                {
                    if (!context.Login.Admin)
                    {
                        user.WriteEventSafe(Output("Permission denied\n"));
                        user.FlushSafeAsync();
                        break;
                    }

                    if (args.Count != 2)
                    {
                        user.WriteEventSafe(Output("Invalid number of arguments, must be <account>\n"));
                        user.FlushSafeAsync();
                        break;
                    }

                    string name = args[1];
                    var toDelete = logins.FirstOrDefault(l => l.User == name);
                    if (toDelete == null)
                    {
                        user.WriteEventSafe(Output("The specified account does not exist\n"));
                        user.FlushSafeAsync();
                        break;
                    }

                    if (toDelete.Admin && login.Person != system.Owner.Key)
                    {
                        user.WriteEventSafe(Output("Only the system owner may delete admin accounts\n"));
                        user.FlushSafeAsync();
                        break;
                    }

                    user.WriteEventSafe(Output($"Are you sure you want to delete account {toDelete.User}?\n"));
                    var confirm = Confirm(user, false);
                    yield return confirm;
                    if (!confirm.Confirmed) yield break;

                    spawn.RemoveLogin(toDelete);
                    break;
                }
                case "list":
                {
                    var sb = new StringBuilder();
                    foreach (var l in logins) sb.Append($"{l.User} ({(l.Admin ? "admin" : "standard")})\n");
                    if (sb.Length == 0) sb.Append('\n');
                    user.WriteEventSafe(Output(sb.ToString()));
                    user.FlushSafeAsync();
                    break;
                }
                default:
                {
                    user.WriteEventSafe(Output("Invalid verb, must be add, list, or delete\n"));
                    user.FlushSafeAsync();
                    break;
                }
            }
        }
    }
}
