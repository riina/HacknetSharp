using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HacknetSharp.Server
{
    public class ServerInstance
    {
        private readonly AccessController _accessController;
        private readonly WorldDatabase _worldDatabase;
        private readonly HashSet<Type> _programTypes;
        private readonly CountdownEvent _countdown;
        private readonly AutoResetEvent _op;
        private State _state;

        protected internal ServerInstance(ServerConfig config)
        {
            var accessControllerType = config.AccessControllerType ??
                                       throw new ArgumentException(
                                           $"{nameof(ServerConfig.AccessControllerType)} not specified");
            var storageContextFactoryType = config.StorageContextFactoryType ??
                                            throw new ArgumentException(
                                                $"{nameof(ServerConfig.StorageContextFactoryType)} not specified");
            _accessController = (AccessController)(Activator.CreateInstance(accessControllerType) ??
                                                   throw new ApplicationException());
            var factory = (StorageContextFactoryBase)(Activator.CreateInstance(storageContextFactoryType) ??
                                                      throw new ApplicationException());
            var context = factory.CreateDbContext(Array.Empty<string>());
            _worldDatabase = new WorldDatabase(context);
            _programTypes = config.Programs;
            _countdown = new CountdownEvent(1);
            _op = new AutoResetEvent(true);
            _state = State.NotStarted;
        }

        public async Task<Task> StartAsync()
        {
            TriggerState(State.NotStarted, State.NotStarted, State.Starting);

            // TODO socket configuration? or clients
            // TODO initialize programs
            TriggerState(State.Starting, State.Starting, State.Active);
            return UpdateLoop();
        }

        private async Task UpdateLoop()
        {
            while (TryIncrementCountdown(State.Active, State.Active))
            {
                // TODO get queued inputs
                // TODO update worlds lockstep

                await Task.Delay(10).Caf();
            }
        }

        public async Task DisposeAsync()
        {
            RequireState(State.Starting, State.Active);
            while (_state != State.Active) await Task.Delay(100).Caf();
            TriggerState(State.Active, State.Active, State.Dispose);
            await Task.Run(() =>
            {
                _op.WaitOne();
                _countdown.Signal();
                _op.Set();
                _countdown.Wait();
            });
            TriggerState(State.Dispose, State.Dispose, State.Disposed);
        }

        private void TriggerState(State min, State max, State target)
        {
            _op.WaitOne();
            try
            {
                RequireState(min, max);
                _state = target;
            }
            finally
            {
                _op.Set();
            }
        }

        private void RequireState(State min, State max)
        {
            if ((int)_state < (int)min)
                throw new InvalidOperationException(
                    $"Cannot perform this action that requires state {min} when object is in state {_state}");
            if ((int)_state > (int)max)
                throw new InvalidOperationException(
                    $"Cannot perform this action that requires state {max} when object is in state {_state}");
        }

        private bool TryIncrementCountdown(State min, State max)
        {
            _op.WaitOne();
            bool keepGoing = !((int)_state < (int)min || (int)_state > (int)max);
            if (keepGoing)
                _countdown.AddCount();
            _op.Set();
            return keepGoing;
        }

        private void DecrementCountdown()
        {
            _op.WaitOne();
            _countdown.Signal();
            _op.Set();
        }

        private enum State
        {
            NotStarted,
            Starting,
            Active,
            Dispose,
            Disposed
        }
    }
}
