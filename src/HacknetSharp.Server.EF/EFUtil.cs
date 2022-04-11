using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace HacknetSharp.Server.EF;

/// <summary>
/// EF utils.
/// </summary>
public static class EFUtil
{
    private static readonly HashSet<Type> _defaultModels =
        new(ServerUtil.GetTypes(typeof(EFModelHelper), typeof(EFUtil).Assembly));

    /// <summary>
    /// Standard model types.
    /// </summary>
    public static IEnumerable<Type> DefaultModels => _defaultModels;

    /// <summary>
    /// Shorthand for ConfigureAwait(false).
    /// </summary>
    /// <param name="task">Task to wrap.</param>
    /// <returns>Wrapped task.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConfiguredValueTaskAwaitable Caf(this ValueTask task) => task.ConfigureAwait(false);

    /// <summary>
    /// Shorthand for ConfigureAwait(false).
    /// </summary>
    /// <param name="task">Task to wrap.</param>
    /// <returns>Wrapped task.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConfiguredValueTaskAwaitable<T> Caf<T>(this ValueTask<T> task) => task.ConfigureAwait(false);

}
