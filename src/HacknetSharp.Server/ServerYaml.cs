namespace HacknetSharp.Server
{
    public class ServerYaml
    {
        public string? Host { get; set; }
        public ushort Port { get; set; } = Constants.DefaultPort;
        public string? DatabaseKind { get; set; }
        public string? SqliteFile { get; set; }
        public string? PostgresHost { get; set; }
        public string? PostgresDatabase { get; set; }
        public string? PostgresUser { get; set; }
        public string? DefaultWorld { get; set; }
        public bool? EnableLogging { get; set; }
    }
}
