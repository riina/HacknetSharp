using System.Collections.Generic;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents an executable that runs in the background.
    /// </summary>
    public abstract class Service : Executable
    {
        /// <summary>
        /// Run this executable with the given context.
        /// </summary>
        /// <param name="context">Context to use with this execution.</param>
        /// <returns>Enumerator that divides execution steps.</returns>
        public abstract IEnumerator<YieldToken?> Run(ServiceContext context);
    }
}
