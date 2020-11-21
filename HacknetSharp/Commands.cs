namespace HacknetSharp
{
    public enum Command : uint
    {
        CS_Disconnect = 0x00_00_00_00,
        CS_Acknowledge = 0x00_00_00_01,
        CS_Register = 0x00_00_00_02,
        CS_Login = 0x00_00_00_03,

        SC_Disconnect = 0x80_00_00_00,
        SC_Acknowledge = 0x80_00_00_01,
        SC_UserInfo = 0x80_00_00_02,
        SC_LoginFail = 0x80_00_00_03,
    }
}
