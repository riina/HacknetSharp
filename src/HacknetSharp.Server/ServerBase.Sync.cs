namespace HacknetSharp.Server
{
    public partial class ServerBase
    {
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
    }
}
