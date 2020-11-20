namespace HacknetSharp
{
    public enum ClientServerCommand : uint
    {
        Disconnect = 0x00_00_00_00,
        Acknowledge = 0x00_00_00_01,
        Register = 0x00_00_00_02,
        Login = 0x00_00_00_03,
    }

    public enum ServerClientCommand : uint
    {
        Disconnect = 0x00_00_00_00,
        Acknowledge = 0x00_00_00_01,
        UserInfo = 0x00_00_00_02,
        LoginFail = 0x00_00_00_03,
    }
}
