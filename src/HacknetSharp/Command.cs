namespace HacknetSharp
{
    public enum Command : uint
    {
        CS_Disconnect = 0x40_00_00_00,
        CS_Login = 0x40_00_00_01,
        CS_InitialCommand = 0x40_00_00_02,
        CS_Command = 0x40_00_00_03,
        CS_RegistrationTokenForgeRequest = 0x40_00_00_04,
        CS_InputResponse = 0x40_00_00_05,
        CS_EditResponse = 0x40_00_00_06,

        SC_Disconnect = 0x80_00_00_00,
        SC_LoginFail = 0x80_00_00_01,
        SC_UserInfo = 0x80_00_00_02,
        SC_AccessFail = 0x80_00_00_03,
        SC_Output = 0x80_00_00_04,
        SC_FailBaseServer = 0x80_00_00_05,
        SC_RegistrationTokenForgeResponse = 0x80_00_00_06,
        SC_OperationComplete = 0x80_00_00_07,
        SC_InputRequest = 0x80_00_00_08,
        SC_EditRequest = 0x80_00_00_09,
    }
}
