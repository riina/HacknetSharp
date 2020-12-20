using System.Collections.Generic;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:login", "login", "login / manage logins",
        "login to system using credentials or\nmanage logins.\n\n" +
        "[name@server]: login to specified target (or\n" +
        "$NAME/$TARGET) using stored credentials\n" +
        "(or $PASS or prompted password)\n\n" +
        "-s [name@server]: save password for specified target (or\n" +
        "$NAME/$TARGET) from $PASS or prompted password\n\n" +
        "-l [name@server]: list passwords for specified target (or\n" +
        "$NAME/$TARGET)\n\n" +
        "-d [name@server]: delete password for specified target (or\n" +
        "$NAME/$TARGET)",
        "[arguments]", false)]
    public class LoginProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var (flags, pargs) = ServerUtil.IsolateFlags(context.Argv);
            if (flags.Contains("s"))
            {
                // TODO save login
            }
            else if (flags.Contains("l"))
            {
                // TODO list login
            }
            else if (flags.Contains("d"))
            {
                // TODO delete login
            }
            else
            {
                // TODO use login
            }
        }
    }
}
