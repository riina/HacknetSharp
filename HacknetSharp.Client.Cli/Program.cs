namespace HacknetSharp.Client.Cli
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var connection = new Connection(args[0], 42069);
        }
    }
}
