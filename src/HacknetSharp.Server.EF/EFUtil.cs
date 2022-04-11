using System;
using System.Collections.Generic;

namespace HacknetSharp.Server.EF;

/// <summary>
/// EF utils.
/// </summary>
public class EFUtil
{
    private static readonly HashSet<Type> _defaultModels =
        new(ServerUtil.GetTypes(typeof(EFModelHelper), typeof(EFUtil).Assembly));

    /// <summary>
    /// Standard model types.
    /// </summary>
    public static IEnumerable<Type> DefaultModels => _defaultModels;

}
