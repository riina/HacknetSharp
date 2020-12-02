using HacknetSharp.Server.Common;

namespace HacknetSharp.Server
{
    public abstract class ExecutableOperation
    {
        /// <summary>
        /// Update current operation.
        /// </summary>
        /// <returns>True when operation is complete.</returns>
        public abstract bool Update(IWorld world);


        /// <summary>
        /// Perform cleanup for operation
        /// </summary>
        public abstract void Complete();
    }
}
