using System;
using System.Collections.Generic;
using Azura;
using HacknetSharp.Server.Models;
using Microsoft.Extensions.Logging;

namespace HacknetSharp.Server.CoreServices
{
    /// <inheritdoc />
    [ServiceInfo("core:chatd", "chatd")]
    public class ChatService : Service
    {
        /// <summary>
        /// Configuration file with <see cref="ChatInfo"/>.
        /// </summary>
        public const string ConfigFile = "chatd.cfg.bin";

        /// <summary>
        /// Message handler delegate.
        /// </summary>
        /// <param name="channel">Message channel, or null if server broadcast.</param>
        /// <param name="sender">Sender model ID.</param>
        /// <param name="name">Sender name.</param>
        /// <param name="message">Message body.</param>
        public delegate void MessageHandlerDelegate(string? channel, Guid sender, string name, string message);

        /// <summary>
        /// Message receiver delegate.
        /// </summary>
        public MessageHandlerDelegate? MessageReceivers { get; set; }

        /// <summary>
        /// Sends a message to all receivers.
        /// </summary>
        /// <param name="channel">Message channel.</param>
        /// <param name="sender">Sender model ID.</param>
        /// <param name="name">Sender name.</param>
        /// <param name="message">Message body.</param>
        public void SendMessage(string channel, Guid sender, string name, string message) =>
            MessageReceivers?.Invoke(channel, sender, name, message);

        /// <summary>
        /// Chat information.
        /// </summary>
        public ChatInfo Info { get; set; } = new();


        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            if (!System.TryAddService(this)) yield break;
            // Try to load chat config
            try
            {
                ChatInfo? info = null;
                if (System.TryGetFile(ConfigFile, Login, out var result, out _, out var file) &&
                    file.Kind == FileModel.FileKind.TextFile)
                {
                    try
                    {
                        info = ServerUtil.ReadBase64Azura(file.Content ?? "", ChatInfoSerialization.Deserialize);
                    }
                    catch
                    {
                        // ignored
                    }
                    if (info == null)
                    {
                        info = new ChatInfo();
                        file.Content = ServerUtil.WriteBase64Azura(info.Serialize);
                        World.Database.Update(file);
                        Info = info;
                    }
                }
                else
                {
                    info = new ChatInfo();
                    if (result == ReadAccessResult.NoExist)
                    {
                        string text = ServerUtil.WriteBase64Azura(info.Serialize);
                        World.Spawn.TextFile(System, Login, ConfigFile, text);
                    }
                    Info = info;
                }
            }
            catch
            {
                // ignored
            }

            while (true)
                yield return null;
        }

        /// <inheritdoc />
        public override bool OnShutdown()
        {
            System.RemoveService(this);
            World.Logger.LogInformation("Chat service shutting down on system {Id}", System.Address);
            MessageReceivers?.Invoke(null, Guid.Empty, "SVCH", "Chat service is shutting down!");
            MessageReceivers = null;
            return true;
        }
    }

    /// <summary>
    /// Stores information about chat service, e.g. available channels and bans.
    /// </summary>
    [Azura]
    public class ChatInfo
    {
        /// <summary>
        /// Available chat rooms, as name-password pairs.
        /// </summary>
        [Azura]
        public Dictionary<string, string> Rooms { get; set; } = new();

        /// <summary>
        /// Banned IP ranges.
        /// </summary>
        [Azura]
        public List<string> Banned { get; set; } = new();

        /// <summary>
        /// Banned IP ranges.
        /// </summary>
        public List<IPAddressRange> BannedRanges { get; set; } = new();
    }
}
