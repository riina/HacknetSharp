using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Base class for asynchronous server.
    /// </summary>
    public class ServerBaseAsync : ServerBase
    {
        private long _ms;
        private long _lastMs;

        /// <summary>
        /// Initializes a new instance of <see cref="ServerBaseAsync"/>.
        /// </summary>
        /// <param name="config">Configuration.</param>
        protected ServerBaseAsync(ServerConfigBase config) : base(config)
        {
        }

        /// <summary>
        /// Starts instance.
        /// </summary>
        /// <returns>Update task.</returns>
        /// <exception cref="InvalidOperationException">Thrown when improperly configured.</exception>
        /// <exception cref="ApplicationException">Thrown for problem in startup.</exception>
        public Task StartAsync()
        {
            StartInternal();
            return UpdateAsync();
        }

        private async Task UpdateAsync()
        {
            long ms = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            _ms = ms;
            _lastMs = ms;
            while (TryIncrementCountdown(LifecycleState.Active, LifecycleState.Active))
            {
                try
                {
                    await UpdateCoreAsync((_ms - _lastMs) / 1000.0f);
                }
                finally
                {
                    DecrementCountdown();
                }
                const int tickMs = 10;
                long ms3 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                await Task.Delay((int)Math.Min(tickMs, Math.Max(0, tickMs - (ms3 - _ms)))).Caf();
                _lastMs = _ms;
                _ms = ms3;
            }
        }

        /// <summary>
        /// Core update.
        /// </summary>
        protected virtual async Task UpdateCoreAsync(float deltaTime)
        {
            UpdateMain(deltaTime);
            await UpdateDatabaseAsync(deltaTime);
        }

        private async Task UpdateDatabaseAsync(float deltaTime)
        {
            if (CheckSave(deltaTime))
                await Database.SyncAsync().Caf();
        }

        /// <summary>
        /// Disconnects active connections.
        /// </summary>
        /// <returns>Task.</returns>
        protected virtual Task DisconnectConnectionsAsync() => Task.CompletedTask;

        /// <summary>
        /// Waits for connection task to end.
        /// </summary>
        /// <returns>Task.</returns>
        protected virtual Task WaitForStopListeningAsync() => Task.CompletedTask;

        /// <inheritdoc />
        public override void Dispose() => DisposeAsync().Wait();

        /// <summary>
        /// Disposes instance.
        /// </summary>
        public async Task DisposeAsync()
        {
            if (State == LifecycleState.Disposed) return;
            Util.RequireState(State, LifecycleState.Starting, LifecycleState.Active);
            while (State != LifecycleState.Active) await Task.Delay(100).Caf();
            Util.TriggerState(_op, LifecycleState.Active, LifecycleState.Active, LifecycleState.Dispose, ref State);
            await DisconnectConnectionsAsync();
            await Task.Run(() =>
            {
                _op.WaitOne();
                _countdown.Signal();
                _op.Set();
                _countdown.Wait();
            }).Caf();
            await WaitForStopListeningAsync();
            Logger.LogInformation("Committing database on close");
            try
            {
                await Database.SyncAsync();
            }
            catch (Exception e)
            {
                Logger.LogWarning("Database commit failed with an exception:\n{Exception}", e);
            }
            finally
            {
                Util.TriggerState(_op, LifecycleState.Dispose, LifecycleState.Dispose, LifecycleState.Disposed,
                    ref State);
            }
        }
    }
}
