using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HacknetSharp.Server.Common;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Contains utility methods.
    /// </summary>
    public static class Util
    {
        /// <summary>
        /// Shorthand for ConfigureAwait(false).
        /// </summary>
        /// <param name="task">Task to wrap.</param>
        /// <returns>Wrapped task.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConfiguredTaskAwaitable Caf(this Task task) => task.ConfigureAwait(false);

        /// <summary>
        /// Shorthand for ConfigureAwait(false).
        /// </summary>
        /// <param name="task">Task to wrap.</param>
        /// <returns>Wrapped task.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConfiguredTaskAwaitable<T> Caf<T>(this Task<T> task) => task.ConfigureAwait(false);

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

        public static IEnumerable<Type> GetTypes(Type t, Assembly assembly) =>
            assembly.GetTypes().Where(type => IsSubclass(t, type));

        internal static readonly HashSet<Type> DefaultModels =
            new HashSet<Type>(GetTypes(typeof(Model<>), typeof(Model<>).Assembly)
                .Concat(GetTypes(typeof(Model<>), typeof(Util).Assembly)));


        internal static readonly HashSet<Type> DefaultPrograms =
            new HashSet<Type>(GetTypes(typeof(Program), typeof(Program).Assembly)
                .Concat(GetTypes(typeof(Program), typeof(Util).Assembly)));

        /// <summary>
        /// Load component types from a folder
        /// </summary>
        /// <param name="folder">Search folder</param>
        /// <param name="models">Model types</param>
        /// <param name="programs">Program types</param>
        public static void LoadTypesFromFolder(string folder, HashSet<Type[]> models,
            HashSet<Type[]> programs)
        {
            if (!Directory.Exists(folder)) return;
            var opts = new EnumerationOptions {MatchCasing = MatchCasing.CaseInsensitive};
            try
            {
                foreach (string d in Directory.GetDirectories(folder))
                {
                    try
                    {
                        string tarName = Path.GetFileName(d);
                        string? fDll = Directory.GetFiles(d, $"{tarName}.dll", opts).FirstOrDefault();
                        if (fDll != null)
                        {
                            var assembly = Assembly.LoadFrom(fDll);
                            models.Add(GetTypes(typeof(Model<>), assembly).ToArray());
                            programs.Add(GetTypes(typeof(Program), assembly).ToArray());
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static bool IsSubclass(Type @base, Type? toCheck) =>
            @base != toCheck && (@base.IsGenericType
                ? IsSubclassOfRawGeneric(@base, toCheck)
                : toCheck?.IsAssignableTo(@base) ?? false);

        // https://stackoverflow.com/a/457708
        private static bool IsSubclassOfRawGeneric(Type generic, Type? toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }

                toCheck = toCheck.BaseType;
            }

            return false;
        }
    }
}
