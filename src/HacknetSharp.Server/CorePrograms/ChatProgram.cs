using System;
using System.Collections.Generic;
using HacknetSharp.Server.CoreServices;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:chat", "chat", "open chat",
        "opens a connection to the specified chatroom",
        "<name> <room@host>", true)]
    public class ChatProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            var (_, _, pargs) = IsolateArgvFlags(Argv);
            if (pargs.Count != 2)
            {
                Write("2 operands are required for this command: <name> <room@host>\n");
                yield break;
            }

            if (!ServerUtil.TryParseConString(pargs[1], 22, out string? room,
                out string? host, out _, out string? error))
            {
                Write($"{error}\n");
                yield break;
            }

            if (!IPAddressRange.TryParse(host, false, out var range) ||
                !range.TryGetIPv4HostAndSubnetMask(out uint hostUint, out _))
            {
                Write($"Invalid host {host}\n");
                yield break;
            }

            Write("Password:");
            var input = Input(true);
            yield return input;
            string password = input.Input!.Input;

            if (!World.Model.AddressedSystems.TryGetValue(hostUint, out var system))
            {
                Write("No route to host\n");
                yield break;
            }

            if (!system.TryGetService(out ChatService? service))
            {
                Write("Chat service not available on server\n");
                yield break;
            }

            var rooms = service.Info.Rooms;

            if (!rooms.TryGetValue(room, out string? roomPassword) || roomPassword != password)
            {
                Write("Invalid credentials\n");
                yield break;
            }

            service.MessageReceivers += OnChat;
            Shell.Chat = service;
            Shell.ChatRoom = room;
            Shell.ChatName = pargs[0];
            SignalUnbindProcess();
            while (true)
                yield return null;
        }

        private void OnChat(string? room, Guid sender, string name, string message)
        {
            if (sender == Login.Key || room != null && Shell.ChatRoom != room) return;
            Write($"\n[{room ?? "BROADCAST"}] {name}: {message}\n")
                .WriteEvent(ServerUtil.CreatePromptEvent(Shell));
        }

        /// <inheritdoc />
        public override bool OnShutdown()
        {
            if (Shell.Chat != null)
            {
                Shell.Chat.MessageReceivers -= OnChat;
                Shell.Chat = null;
                Shell.ChatRoom = null;
                Shell.ChatName = null;
            }

            return true;
        }
    }
}
