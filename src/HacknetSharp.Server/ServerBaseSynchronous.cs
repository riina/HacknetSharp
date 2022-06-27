using System;
using Microsoft.Extensions.Logging;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Base class for synchronous server.
    /// </summary>
    public abstract class ServerBaseSynchronous : ServerBase
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ServerBaseSynchronous"/>.
        /// </summary>
        /// <param name="config">Configuration.</param>
        protected ServerBaseSynchronous(ServerConfigBase config) : base(config)
        {
        }

        /// <summary>
        /// Starts instance.
        /// </summary>
        /// <returns>Update task.</returns>
        /// <exception cref="InvalidOperationException">Thrown when improperly configured.</exception>
        /// <exception cref="ApplicationException">Thrown for problem in startup.</exception>
        public void Start()
        {
            StartInternal();
        }

        /// <summary>
        /// Manually updates server.
        /// </summary>
        /// <param name="deltaTime">Delta time.</param>
        public void Update(float deltaTime)
        {
            if (TryIncrementCountdown(LifecycleState.Active, LifecycleState.Active))
            {
                try
                {
                    UpdateCore(deltaTime);
                }
                finally
                {
                    DecrementCountdown();
                }
            }
        }

        /// <summary>
        /// Core update.
        /// </summary>
        protected virtual void UpdateCore(float deltaTime)
        {
            UpdateMain(deltaTime);
            UpdateDatabase(deltaTime);
        }

        private void UpdateDatabase(float deltaTime)
        {
            if (CheckSave(deltaTime)) Database.Sync();
        }

        /// <summary>
        /// Disconnects active connections.
        /// </summary>
        /// <returns>Task.</returns>
        protected virtual void DisconnectConnections()
        {
        }

        /// <summary>
        /// Waits for connection task to end.
        /// </summary>
        /// <returns>Task.</returns>
        protected virtual void WaitForStopListening()
        {
        }

        /// <summary>
        /// Disposes instance.
        /// </summary>
        public virtual void Dispose()
        {
            if (State == LifecycleState.Disposed) return;
            Util.RequireState(State, LifecycleState.Active, LifecycleState.Active);
            Util.TriggerState(_op, LifecycleState.Active, LifecycleState.Active, LifecycleState.Dispose, ref State);
            DisconnectConnections();
            _op.WaitOne();
            _countdown.Signal();
            _op.Set();
            _countdown.Wait();
            WaitForStopListening();
            Logger.LogInformation("Committing database on close");
            try
            {
                Database.Sync();
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
