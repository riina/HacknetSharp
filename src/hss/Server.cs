using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using HacknetSharp;
using HacknetSharp.Events.Server;
using HacknetSharp.Server;
using HacknetSharp.Server.EF;
using HacknetSharp.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace hss
{
    public class Server : ServerBaseAsync
    {
        private static readonly CultureInfo s_ic = CultureInfo.InvariantCulture;

        internal X509Certificate Cert { get; }
        internal ServerDatabase EfDatabase { get; }
        internal AccessController AccessController { get; }
        private readonly ConcurrentDictionary<Guid, HostConnection> _connections;
        private readonly TcpListener _connectListener;
        private Task? _connectTask;

        protected internal Server(ServerConfig config) : base(config)
        {
            Cert = config.Certificate ?? throw new ArgumentException(
                $"{nameof(ServerConfig.Certificate)} not specified");
            var factory = config.StorageContextFactory ??
                          throw new ArgumentException($"{nameof(ServerConfig.StorageContextFactory)} not specified");
            var context = factory.CreateDbContext(Array.Empty<string>());
            EfDatabase = new ServerDatabase(context);
            ConfigureDatabase(EfDatabase); // configure before worlds
            AccessController = new AccessController(this);
            World? defaultWorld = null;
            foreach (var w in context.Set<WorldModel>())
            {
                var world = new World(this, w);
                Worlds[w.Key] = world;
                if (w.Name.Equals(config.DefaultWorld)) defaultWorld = world;
            }
            ConfigureDefaultWorld(defaultWorld ?? throw new ApplicationException("No world matching name found"));
            _connectListener = new TcpListener(IPAddress.Any, config.Port);
            _connections = new ConcurrentDictionary<Guid, HostConnection>();
        }

        private async Task RunConnectListener()
        {
            try
            {
                while (TryIncrementCountdown(LifecycleState.Active, LifecycleState.Active))
                {
                    try
                    {
                        var connection = new HostConnection(this, await _connectListener.AcceptTcpClientAsync().Caf());
                        _connections.TryAdd(connection.Id, connection);
                    }
                    catch (IOException ioe)
                    {
                        Logger.LogWarning("Connection listener threw an IO exception:\n{Exception}", ioe);
                        return;
                    }
                    catch (SocketException se)
                    {
                        if (se.SocketErrorCode == SocketError.OperationAborted)
                            return;
                        else
                            Logger.LogWarning("Connection listener threw a socket exception:\n{Exception}", se);
                    }
                    finally
                    {
                        DecrementCountdown();
                    }
                }
            }
            finally
            {
                if (State >= LifecycleState.Dispose)
                    Logger.LogInformation("Connection listener is offline");
                else
                    Logger.LogWarning("Connection listener has closed sooner than expected");
            }
        }

        protected override async Task UpdateCoreAsync(float deltaTime)
        {
            try
            {
                await base.UpdateCoreAsync(deltaTime);
            }
            catch (DbUpdateConcurrencyException e)
            {
                var sb = new StringBuilder();
                foreach (var x in e.Entries)
                {
                    sb.AppendLine(s_ic, $"{x.Entity}");
                    switch (x.Entity)
                    {
                        case FileModel y:
                            sb.AppendLine(s_ic, $"[{y.Path}] [{y.Name}]");
                            sb.AppendLine("Current:");
                            foreach (var z in x.CurrentValues.Properties)
                                sb.AppendLine(s_ic, $"{z.Name} // {z} // [{x.CurrentValues[z]}] vs [{x.OriginalValues[z]}]");
                            break;
                    }
                }
                Logger.LogError(
                    $"{nameof(DbUpdateConcurrencyException)} thrown in server loop:\n{{Exception}}\nDetails:\n{{Information}}",
                    e, sb.ToString());
            }
        }

        protected override void StartListening()
        {
            _connectListener.Start();
            _connectTask = RunConnectListener();
        }

        protected override void DisconnectConnections()
        {
            var connectionIds = _connections.Keys;
            _connectListener.Stop();
            foreach (var id in connectionIds)
            {
                Logger.LogInformation("Disconnecting connection {Id} for server dispose", id);
                DisconnectConnectionAsync(id).Wait();
            }
        }

        protected override async Task DisconnectConnectionsAsync()
        {
            var connectionIds = _connections.Keys;
            _connectListener.Stop();
            foreach (var id in connectionIds)
            {
                Logger.LogInformation("Disconnecting connection {Id} for server dispose", id);
                await DisconnectConnectionAsync(id);
            }
        }

        protected override void WaitForStopListening()
        {
            _connectTask?.Wait();
        }

        protected override async Task WaitForStopListeningAsync() => await (_connectTask ?? Task.CompletedTask);

        public bool HasConnection(string userName) => _connections.Any(c => c.Value.UserName == userName);

        internal void SelfRemoveConnection(Guid id) => _connections.TryRemove(id, out _);

        private async Task DisconnectConnectionAsync(Guid id)
        {
            if (_connections.TryRemove(id, out var connection))
            {
                try
                {
                    connection.WriteEventSafe(new ServerDisconnectEvent { Reason = "Server shutting down." });
                    await connection.FlushSafeAsync();
                    connection.Dispose();
                }
                catch
                {
                    // ignored
                }
            }
        }

        internal bool TryIncrementCountdownForHostConnection(LifecycleState min, LifecycleState max) =>
            TryIncrementCountdown(min, max);

        internal void DecrementCountdownForHostConnection() => DecrementCountdown();
    }
}
