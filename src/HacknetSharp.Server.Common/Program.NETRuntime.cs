using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace HacknetSharp.Server.Common
{
    public partial class Program
    {
        /*
         * Code pulled from https://github.com/dotnet/runtime
         * Current as of 17f5167d75bba8d53ed6a276f010ac51999f7983
         * Modifications
         * - GetFullPath doesn't try to root with cwd
         * - Manual constants for resource strings
         */

        #region Code from .NET Runtime library

        private static class SR
        {
            internal const string Arg_PathEmpty = "The path is empty.";
            internal const string Argument_InvalidPathChars = "Illegal characters in path.";
            internal const string Arg_BasePathNotFullyQualified = "Basepath argument is not fully qualified.";
        }

        /// <summary>
        /// Returns the directory portion of a file path. This method effectively
        /// removes the last segment of the given file path, i.e. it returns a
        /// string consisting of all characters up to but not including the last
        /// backslash ("\") in the file path. The returned value is null if the
        /// specified path is null, empty, or a root (such as "\", "C:", or
        /// "\\server\share").
        /// </summary>
        /// <remarks>
        /// Directory separators are normalized in the returned string.
        /// </remarks>
        public static string? GetDirectoryName(string? path)
        {
            if (path == null || PathInternal.IsEffectivelyEmpty(path.AsSpan()))
                return null;

            int end = GetDirectoryNameOffset(path.AsSpan());
            return end >= 0 ? PathInternal.NormalizeDirectorySeparators(path.Substring(0, end)) : null;
        }

        private static int GetDirectoryNameOffset(ReadOnlySpan<char> path)
        {
            int rootLength = PathInternal.GetRootLength(path);
            int end = path.Length;
            if (end <= rootLength)
                return -1;

            // ReSharper disable once EmptyEmbeddedStatement
            while (end > rootLength && !PathInternal.IsDirectorySeparator(path[--end])) ;

            // Trim off any remaining separators (to deal with C:\foo\\bar)
            while (end > rootLength && PathInternal.IsDirectorySeparator(path[end - 1]))
                end--;

            return end;
        }

        /// <summary>
        /// Returns true if the path is fixed to a specific drive or UNC path. This method does no
        /// validation of the path (URIs will be returned as relative as a result).
        /// Returns false if the path specified is relative to the current drive or working directory.
        /// </summary>
        /// <remarks>
        /// Handles paths that use the alternate directory separator.  It is a frequent mistake to
        /// assume that rooted paths <see cref="Path.IsPathRooted(string)"/> are not relative.  This isn't the case.
        /// "C:a" is drive relative- meaning that it will be resolved against the current directory
        /// for C: (rooted, but relative). "C:\a" is rooted and not relative (the current directory
        /// will not be used to modify the path).
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="path"/> is null.
        /// </exception>
        public static bool IsPathFullyQualified(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return IsPathFullyQualified(path.AsSpan());
        }

        public static bool IsPathFullyQualified(ReadOnlySpan<char> path)
        {
            return !PathInternal.IsPartiallyQualified(path);
        }

        public static string Combine(string path1, string path2)
        {
            if (path1 == null || path2 == null)
                throw new ArgumentNullException((path1 == null) ? nameof(path1) : nameof(path2));

            return CombineInternal(path1, path2);
        }

        public static string Combine(string path1, string path2, string path3)
        {
            if (path1 == null || path2 == null || path3 == null)
                throw new ArgumentNullException((path1 == null) ? nameof(path1) :
                    (path2 == null) ? nameof(path2) : nameof(path3));

            return CombineInternal(path1, path2, path3);
        }

        public static string Combine(string path1, string path2, string path3, string path4)
        {
            if (path1 == null || path2 == null || path3 == null || path4 == null)
                throw new ArgumentNullException((path1 == null) ? nameof(path1) :
                    (path2 == null) ? nameof(path2) :
                    (path3 == null) ? nameof(path3) : nameof(path4));

            return CombineInternal(path1, path2, path3, path4);
        }

        public static string Combine(params string[] paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            int maxSize = 0;
            int firstComponent = 0;

            // We have two passes, the first calculates how large a buffer to allocate and does some precondition
            // checks on the paths passed in.  The second actually does the combination.

            for (int i = 0; i < paths.Length; i++)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (paths[i] == null)
                {
                    throw new ArgumentNullException(nameof(paths));
                }

                if (paths[i].Length == 0)
                {
                    continue;
                }

                if (IsPathRooted(paths[i]))
                {
                    firstComponent = i;
                    maxSize = paths[i].Length;
                }
                else
                {
                    maxSize += paths[i].Length;
                }

                char ch = paths[i][paths[i].Length - 1];
                if (!PathInternal.IsDirectorySeparator(ch))
                    maxSize++;
            }

            var builder = new ValueStringBuilder(stackalloc char[260]); // MaxShortPath on Windows
            builder.EnsureCapacity(maxSize);

            for (int i = firstComponent; i < paths.Length; i++)
            {
                if (paths[i].Length == 0)
                {
                    continue;
                }

                if (builder.Length == 0)
                {
                    builder.Append(paths[i]);
                }
                else
                {
                    char ch = builder[builder.Length - 1];
                    if (!PathInternal.IsDirectorySeparator(ch))
                    {
                        builder.Append(PathInternal.DirectorySeparatorChar);
                    }

                    builder.Append(paths[i]);
                }
            }

            return builder.ToString();
        }

        // Unlike Combine(), Join() methods do not consider rooting. They simply combine paths, ensuring that there
        // is a directory separator between them.

        public static string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2)
        {
            if (path1.Length == 0)
                return path2.ToString();
            if (path2.Length == 0)
                return path1.ToString();

            return JoinInternal(path1, path2);
        }

        public static string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3)
        {
            if (path1.Length == 0)
                return Join(path2, path3);

            if (path2.Length == 0)
                return Join(path1, path3);

            if (path3.Length == 0)
                return Join(path1, path2);

            return JoinInternal(path1, path2, path3);
        }

        public static string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3,
            ReadOnlySpan<char> path4)
        {
            if (path1.Length == 0)
                return Join(path2, path3, path4);

            if (path2.Length == 0)
                return Join(path1, path3, path4);

            if (path3.Length == 0)
                return Join(path1, path2, path4);

            if (path4.Length == 0)
                return Join(path1, path2, path3);

            return JoinInternal(path1, path2, path3, path4);
        }

        public static string Join(string? path1, string? path2)
        {
            return Join(path1.AsSpan(), path2.AsSpan());
        }

        public static string Join(string? path1, string? path2, string? path3)
        {
            return Join(path1.AsSpan(), path2.AsSpan(), path3.AsSpan());
        }

        public static string Join(string? path1, string? path2, string? path3, string? path4)
        {
            return Join(path1.AsSpan(), path2.AsSpan(), path3.AsSpan(), path4.AsSpan());
        }

        public static string Join(params string?[] paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            if (paths.Length == 0)
            {
                return string.Empty;
            }

            int maxSize = 0;
            foreach (string? path in paths)
            {
                maxSize += path?.Length ?? 0;
            }

            maxSize += paths.Length - 1;

            var builder = new ValueStringBuilder(stackalloc char[260]); // MaxShortPath on Windows
            builder.EnsureCapacity(maxSize);

            for (int i = 0; i < paths.Length; i++)
            {
                string? path = paths[i];
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (builder.Length == 0)
                {
                    builder.Append(path);
                }
                else
                {
                    if (!PathInternal.IsDirectorySeparator(builder[builder.Length - 1]) &&
                        !PathInternal.IsDirectorySeparator(path[0]))
                    {
                        builder.Append(PathInternal.DirectorySeparatorChar);
                    }

                    builder.Append(path);
                }
            }

            return builder.ToString();
        }

        private static string CombineInternal(string first, string second)
        {
            if (string.IsNullOrEmpty(first))
                return second;

            if (string.IsNullOrEmpty(second))
                return first;

            if (IsPathRooted(second.AsSpan()))
                return second;

            return JoinInternal(first.AsSpan(), second.AsSpan());
        }

        private static string CombineInternal(string first, string second, string third)
        {
            if (string.IsNullOrEmpty(first))
                return CombineInternal(second, third);
            if (string.IsNullOrEmpty(second))
                return CombineInternal(first, third);
            if (string.IsNullOrEmpty(third))
                return CombineInternal(first, second);

            if (IsPathRooted(third.AsSpan()))
                return third;
            if (IsPathRooted(second.AsSpan()))
                return CombineInternal(second, third);

            return JoinInternal(first.AsSpan(), second.AsSpan(), third.AsSpan());
        }

        private static string CombineInternal(string first, string second, string third, string fourth)
        {
            if (string.IsNullOrEmpty(first))
                return CombineInternal(second, third, fourth);
            if (string.IsNullOrEmpty(second))
                return CombineInternal(first, third, fourth);
            if (string.IsNullOrEmpty(third))
                return CombineInternal(first, second, fourth);
            if (string.IsNullOrEmpty(fourth))
                return CombineInternal(first, second, third);

            if (IsPathRooted(fourth.AsSpan()))
                return fourth;
            if (IsPathRooted(third.AsSpan()))
                return CombineInternal(third, fourth);
            if (IsPathRooted(second.AsSpan()))
                return CombineInternal(second, third, fourth);

            return JoinInternal(first.AsSpan(), second.AsSpan(), third.AsSpan(), fourth.AsSpan());
        }

        private static unsafe string JoinInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
        {
            Debug.Assert(first.Length > 0 && second.Length > 0, "should have dealt with empty paths");

            bool hasSeparator = PathInternal.IsDirectorySeparator(first[first.Length - 1])
                                || PathInternal.IsDirectorySeparator(second[0]);

            fixed (char* f = &MemoryMarshal.GetReference(first), s = &MemoryMarshal.GetReference(second))
            {
#if MS_IO_REDIST
                return StringExtensions.Create(
#else
                return string.Create(
#endif
                    first.Length + second.Length + (hasSeparator ? 0 : 1),
                    (First: (IntPtr)f, FirstLength: first.Length, Second: (IntPtr)s, SecondLength: second.Length,
                        HasSeparator: hasSeparator),
                    (destination, state) =>
                    {
                        new Span<char>((char*)state.First, state.FirstLength).CopyTo(destination);
                        if (!state.HasSeparator)
                            destination[state.FirstLength] = PathInternal.DirectorySeparatorChar;
                        new Span<char>((char*)state.Second, state.SecondLength).CopyTo(
                            destination.Slice(state.FirstLength + (state.HasSeparator ? 0 : 1)));
                    });
            }
        }

        private static unsafe string JoinInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second,
            ReadOnlySpan<char> third)
        {
            Debug.Assert(first.Length > 0 && second.Length > 0 && third.Length > 0,
                "should have dealt with empty paths");

            bool firstHasSeparator = PathInternal.IsDirectorySeparator(first[first.Length - 1])
                                     || PathInternal.IsDirectorySeparator(second[0]);
            bool thirdHasSeparator = PathInternal.IsDirectorySeparator(second[second.Length - 1])
                                     || PathInternal.IsDirectorySeparator(third[0]);

            fixed (char* f = &MemoryMarshal.GetReference(first), s = &MemoryMarshal.GetReference(second), t =
                &MemoryMarshal.GetReference(third))
            {
#if MS_IO_REDIST
                return StringExtensions.Create(
#else
                return string.Create(
#endif
                    first.Length + second.Length + third.Length + (firstHasSeparator ? 0 : 1) +
                    (thirdHasSeparator ? 0 : 1),
                    (First: (IntPtr)f, FirstLength: first.Length, Second: (IntPtr)s, SecondLength: second.Length,
                        Third: (IntPtr)t, ThirdLength: third.Length, FirstHasSeparator: firstHasSeparator,
                        ThirdHasSeparator: thirdHasSeparator),
                    (destination, state) =>
                    {
                        new Span<char>((char*)state.First, state.FirstLength).CopyTo(destination);
                        if (!state.FirstHasSeparator)
                            destination[state.FirstLength] = PathInternal.DirectorySeparatorChar;
                        new Span<char>((char*)state.Second, state.SecondLength).CopyTo(
                            destination.Slice(state.FirstLength + (state.FirstHasSeparator ? 0 : 1)));
                        if (!state.ThirdHasSeparator)
                            destination[destination.Length - state.ThirdLength - 1] =
                                PathInternal.DirectorySeparatorChar;
                        new Span<char>((char*)state.Third, state.ThirdLength).CopyTo(
                            destination.Slice(destination.Length - state.ThirdLength));
                    });
            }
        }

        private static unsafe string JoinInternal(ReadOnlySpan<char> first, ReadOnlySpan<char> second,
            ReadOnlySpan<char> third, ReadOnlySpan<char> fourth)
        {
            Debug.Assert(first.Length > 0 && second.Length > 0 && third.Length > 0 && fourth.Length > 0,
                "should have dealt with empty paths");

            bool firstHasSeparator = PathInternal.IsDirectorySeparator(first[first.Length - 1])
                                     || PathInternal.IsDirectorySeparator(second[0]);
            bool thirdHasSeparator = PathInternal.IsDirectorySeparator(second[second.Length - 1])
                                     || PathInternal.IsDirectorySeparator(third[0]);
            bool fourthHasSeparator = PathInternal.IsDirectorySeparator(third[third.Length - 1])
                                      || PathInternal.IsDirectorySeparator(fourth[0]);

            fixed (char* f = &MemoryMarshal.GetReference(first), s = &MemoryMarshal.GetReference(second), t =
                &MemoryMarshal.GetReference(third), u = &MemoryMarshal.GetReference(fourth))
            {
#if MS_IO_REDIST
                return StringExtensions.Create(
#else
                return string.Create(
#endif
                    first.Length + second.Length + third.Length + fourth.Length + (firstHasSeparator ? 0 : 1) +
                    (thirdHasSeparator ? 0 : 1) + (fourthHasSeparator ? 0 : 1),
                    (First: (IntPtr)f, FirstLength: first.Length, Second: (IntPtr)s, SecondLength: second.Length,
                        Third: (IntPtr)t, ThirdLength: third.Length, Fourth: (IntPtr)u, FourthLength: fourth.Length,
                        FirstHasSeparator: firstHasSeparator, ThirdHasSeparator: thirdHasSeparator,
                        FourthHasSeparator: fourthHasSeparator),
                    (destination, state) =>
                    {
                        new Span<char>((char*)state.First, state.FirstLength).CopyTo(destination);
                        if (!state.FirstHasSeparator)
                            destination[state.FirstLength] = PathInternal.DirectorySeparatorChar;
                        new Span<char>((char*)state.Second, state.SecondLength).CopyTo(
                            destination.Slice(state.FirstLength + (state.FirstHasSeparator ? 0 : 1)));
                        if (!state.ThirdHasSeparator)
                            destination[state.FirstLength + state.SecondLength + (state.FirstHasSeparator ? 0 : 1)] =
                                PathInternal.DirectorySeparatorChar;
                        new Span<char>((char*)state.Third, state.ThirdLength).CopyTo(
                            destination.Slice(state.FirstLength + state.SecondLength +
                                              (state.FirstHasSeparator ? 0 : 1) + (state.ThirdHasSeparator ? 0 : 1)));
                        if (!state.FourthHasSeparator)
                            destination[destination.Length - state.FourthLength - 1] =
                                PathInternal.DirectorySeparatorChar;
                        new Span<char>((char*)state.Fourth, state.FourthLength).CopyTo(
                            destination.Slice(destination.Length - state.FourthLength));
                    });
            }
        }

        // Expands the given path to a fully qualified path.
        public static string GetFullPath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (path.Length == 0)
                throw new ArgumentException(SR.Arg_PathEmpty, nameof(path));

            if (path.Contains('\0'))
                throw new ArgumentException(SR.Argument_InvalidPathChars, nameof(path));

            // Expand with current directory if necessary
            if (!IsPathRooted(path))
            {
                //path = Combine(Interop.Sys.GetCwd(), path);
            }

            // We would ideally use realpath to do this, but it resolves symlinks, requires that the file actually exist,
            // and turns it into a full path, which we only want if fullCheck is true.
            string collapsedString = PathInternal.RemoveRelativeSegments(path, PathInternal.GetRootLength(path));

            // ReSharper disable once RedundantToStringCall
            Debug.Assert(collapsedString.Length < path.Length || collapsedString.ToString() == path,
                "Either we've removed characters, or the string should be unmodified from the input path.");

            string result = collapsedString.Length == 0 ? PathInternal.DirectorySeparatorCharAsString : collapsedString;

            return result;
        }

        public static string GetFullPath(string path, string basePath)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (basePath == null)
                throw new ArgumentNullException(nameof(basePath));

            if (!IsPathFullyQualified(basePath))
                throw new ArgumentException(SR.Arg_BasePathNotFullyQualified, nameof(basePath));

            if (basePath.Contains('\0') || path.Contains('\0'))
                throw new ArgumentException(SR.Argument_InvalidPathChars);

            if (IsPathFullyQualified(path))
                return GetFullPath(path);

            return GetFullPath(CombineInternal(basePath, path));
        }

        public static bool IsPathRooted(ReadOnlySpan<char> path)
        {
            return path.Length > 0 && path[0] == PathInternal.DirectorySeparatorChar;
        }

        #endregion
    }
}
