using HacknetSharp.Events.Client;
using HacknetSharp.Events.Server;

namespace HacknetSharp
{
    /// <summary>
    /// Represents a network command code.
    /// </summary>
    public enum Command : uint
    {
        /// <summary>
        /// Corresponds to <see cref="ClientDisconnectEvent"/>
        /// </summary>
        CS_Disconnect = 0x40_00_00_00,

        /// <summary>
        /// Corresponds to <see cref="LoginEvent"/>
        /// </summary>
        CS_Login = 0x40_00_00_01,

        /// <summary>
        /// Corresponds to <see cref="InitialCommandEvent"/>
        /// </summary>
        CS_InitialCommand = 0x40_00_00_02,

        /// <summary>
        /// Corresponds to <see cref="CommandEvent"/>
        /// </summary>
        CS_Command = 0x40_00_00_03,

        /// <summary>
        /// Corresponds to <see cref="RegistrationTokenForgeRequestEvent"/>
        /// </summary>
        CS_RegistrationTokenForgeRequest = 0x40_00_00_04,

        /// <summary>
        /// Corresponds to <see cref="InputResponseEvent"/>
        /// </summary>
        CS_InputResponse = 0x40_00_00_05,

        /// <summary>
        /// Corresponds to <see cref="EditResponseEvent"/>
        /// </summary>
        CS_EditResponse = 0x40_00_00_06,

        /// <summary>
        /// Corresponds to <see cref="ServerDisconnectEvent"/>
        /// </summary>
        SC_Disconnect = 0x80_00_00_00,

        /// <summary>
        /// Corresponds to <see cref="LoginFailEvent"/>
        /// </summary>
        SC_LoginFail = 0x80_00_00_01,

        /// <summary>
        /// Corresponds to <see cref="UserInfoEvent"/>
        /// </summary>
        SC_UserInfo = 0x80_00_00_02,

        /// <summary>
        /// Corresponds to <see cref="AccessFailEvent"/>
        /// </summary>
        SC_AccessFail = 0x80_00_00_03,

        /// <summary>
        /// Corresponds to <see cref="OutputEvent"/>
        /// </summary>
        SC_Output = 0x80_00_00_04,

        /// <summary>
        /// Corresponds to <see cref="FailBaseServerEvent"/>
        /// </summary>
        SC_FailBaseServer = 0x80_00_00_05,

        /// <summary>
        /// Corresponds to <see cref="RegistrationTokenForgeResponseEvent"/>
        /// </summary>
        SC_RegistrationTokenForgeResponse = 0x80_00_00_06,

        /// <summary>
        /// Corresponds to <see cref="OperationCompleteEvent"/>
        /// </summary>
        SC_OperationComplete = 0x80_00_00_07,

        /// <summary>
        /// Corresponds to <see cref="InputRequestEvent"/>
        /// </summary>
        SC_InputRequest = 0x80_00_00_08,

        /// <summary>
        /// Corresponds to <see cref="EditRequestEvent"/>
        /// </summary>
        SC_EditRequest = 0x80_00_00_09,
    }
}
