using System;
using System.Collections.Generic;
using System.Linq;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Utility for generating rudimentary glob-ish filters.
    /// </summary>
    public static class PathFilter
    {
        /// <summary>
        /// Generate filter for filter strings
        /// </summary>
        /// <param name="filterStrings"></param>
        /// <param name="infiniteRoot">Filters only verify tail</param>
        /// <returns>Filter</returns>
        public static Filter GenerateFilter(IEnumerable<string> filterStrings, bool infiniteRoot = false)
        {
            var filters = new List<Func<ReadOnlyMemory<string>, FilterType>>();
            foreach (string add in filterStrings)
            {
                // Get any leading ! to ignore for main filters
                int sc = 0;
                foreach (char c in add)
                    if (c == '!')
                        sc++;
                    else
                        break;
                string xAdd = sc == 0 ? add.StartsWith("\\") ? add.Substring(1) : add : add.Substring(sc);
                if (string.IsNullOrWhiteSpace(xAdd)) continue; // Blank filter - ignore
                var xAddSplit = xAdd.Split('/');
                Func<ReadOnlyMemory<string>, FilterType> func = F_None; // Default deny
                for (int i = xAddSplit.Length - 1; i >= 0; i--)
                {
                    switch (xAddSplit[i])
                    {
                        case "**":
                        {
                            // Make sure any sequential * / ** are handled together
                            int j = i - 1;
                            while (j >= 0 && (xAddSplit[j] == "**" || xAddSplit[j] == "*"))
                                j--;
                            // Any if at top of chain, otherwise existing
                            func = F_DeepAny(i == xAddSplit.Length - 1 ? F_Any : func);
                            i = j + 1;
                            break;
                        }
                        case "*":
                        {
                            // Make sure any sequential * / ** are handled together
                            int j = i - 1;
                            while (j >= 0 && (xAddSplit[j] == "**" || xAddSplit[j] == "*"))
                                j--;
                            // If only one *, single any, otherwise [any if at top of chain, otherwise existing]
                            func = j == i - 1 ? F_Any(func) : F_DeepAny(i == xAddSplit.Length - 1 ? F_Any : func);
                            i = j + 1;
                            break;
                        }
                        default:
                            if (xAddSplit[i] == string.Empty)
                            {
                                if (i == xAddSplit.Length - 1)
                                    func = F_DeepAny(func == F_None ? F_Any : func);
                            }
                            else
                                // Simple match
                                func = F_Match(xAddSplit[i], func == F_None ? F_Any : func);

                            break;
                    }
                }

                if (infiniteRoot && !xAdd.StartsWith("/"))
                    func = F_DeepAny(func);
                // If odd number of ! is prefixed, invert filter
                if (sc % 2 == 1)
                    func = F_Invert(func);
                filters.Add(func);
            }

            return new Filter(filters);
        }

        private static FilterType F_Any(ReadOnlyMemory<string> arg) => FilterType.Affirm;

        private static FilterType F_None(ReadOnlyMemory<string> arg) =>
            arg.Length == 0 ? FilterType.Affirm : FilterType.NoMatch;

        private static Func<ReadOnlyMemory<string>, FilterType>
            F_Match(string pattern, Func<ReadOnlyMemory<string>, FilterType> after)
        {
            if (pattern.Length == 0) return x => x.Length == 0 ? FilterType.Affirm : FilterType.NoMatch;
            // #FrontierSetter
            int sCount1 = 1;
            int sLast = -1;
            for (int i = 0; i < pattern.Length; i++)
            {
                if (pattern[i] == '*') continue;
                if (sLast + 1 != i)
                    sCount1++;
                sLast = i;
            }

            if (sLast != pattern.Length - 1)
                sCount1++;

            int fixedLength = GetFixedLength(pattern.AsSpan());

            if (sCount1 == 1)
                return path => FixedIndexOf(path.Span[0].AsSpan(), pattern.AsSpan()) == 0 &&
                               fixedLength == path.Span[0].Length
                    ? after.Invoke(path.Slice(1))
                    : FilterType.NoMatch;

            var info = new int[sCount1 * 3];
            // State buffer, 3 bytes per split
            // 0: Offset of pattern
            // 1: Length of pattern
            // 2: Fixed length of input

            int idx1 = 0;
            sLast = -1;
            for (int i = 0; i < pattern.Length; i++)
            {
                if (pattern[i] == '*') continue;
                if (sLast + 1 != i)
                {
                    idx1++;
                    info[idx1 * 3] = i;
                }

                sLast = i;
                info[idx1 * 3 + 1]++;
            }

            for (int i = 0; i < sCount1; i++)
                info[i * 3 + 2] = GetFixedLength(pattern.AsSpan().Slice(info[i * 3], info[i * 3 + 1]));

            return path =>
            {
                var strSpan = path.Span[0].AsSpan();
                var patternSpan = pattern.AsSpan();
                int idx = 0;
                int sLoc = 0;
                int sCount = info.Length / 3;
                while (true)
                {
                    int pos = idx == sCount - 1
                        ? FixedLastIndexOf(strSpan.Slice(sLoc), patternSpan.Slice(info[idx * 3], info[idx * 3 + 1]))
                        : FixedIndexOf(strSpan.Slice(sLoc), patternSpan.Slice(info[idx * 3], info[idx * 3 + 1]));
                    if (pos == -1) return FilterType.NoMatch;
                    if (idx == sCount - 1)
                        // Current is match if not final pattern or if hit end of string (valid since case of 1 split is already handled)
                        if (info[idx * 3] + info[idx * 3 + 1] != patternSpan.Length ||
                            sLoc + pos + info[idx * 3 + 2] == strSpan.Length)
                            return after.Invoke(path.Slice(1));
                        else
                            return FilterType.NoMatch;

                    sLoc += info[idx * 3 + 2];
                    idx++;
                }
            };
        }

        private static int GetFixedLength(ReadOnlySpan<char> pattern)
        {
            int count = 0;
            for (int rCount = 0; rCount < pattern.Length;)
            {
                switch (pattern[rCount])
                {
                    case '[':
                        int eLoc = pattern.Slice(rCount).IndexOf(']');
                        if (eLoc == -1) throw new ApplicationException("Missing end sqbracket");
                        count++;
                        rCount += eLoc + 1;
                        break;
                    default:
                        count++;
                        rCount++;
                        break;
                }
            }

            return count;
        }

        private static int FixedIndexOf(ReadOnlySpan<char> text, ReadOnlySpan<char> pattern)
        {
            if (pattern.Length == 0) return 0;
            if (text.Length == 0) return -1;
            int baseIdx = 0;
            int curTextIdx = 0;
            int curPatternIdx = 0;
            while (curTextIdx < text.Length)
            {
                bool keep = true;
                char c = pattern[curPatternIdx];
                switch (c)
                {
                    case '?':
                        curTextIdx++;
                        curPatternIdx++;
                        break;
                    case '[':
                        int eLoc = pattern.Slice(curPatternIdx).IndexOf(']'); // No escaping concept
                        if (eLoc == -1) throw new ApplicationException("Missing end sqbracket");
                        eLoc--; // Now count of elements
                        if (pattern.Slice(curPatternIdx + 1, eLoc).IndexOf(text[curTextIdx]) != -1)
                        {
                            curTextIdx++;
                            curPatternIdx += eLoc + 2;
                        }
                        else
                            keep = false;

                        break;
                    default:
                        if (c == text[curTextIdx])
                        {
                            curTextIdx++;
                            curPatternIdx++;
                        }
                        else
                            keep = false;

                        break;
                }

                if (curPatternIdx == pattern.Length) return baseIdx;

                if (keep) continue;

                baseIdx++;
                if (baseIdx >= text.Length) return -1;
                curTextIdx = baseIdx;
                curPatternIdx = 0;
            }

            return -1;
        }

        private static int FixedLastIndexOf(ReadOnlySpan<char> text, ReadOnlySpan<char> pattern)
        {
            if (pattern.Length == 0) return text.Length - 1;
            if (text.Length == 0) return -1;
            int baseIdx = text.Length - 1;
            int curTextIdx = text.Length - 1;
            int curPatternIdx = pattern.Length - 1;
            while (curTextIdx >= 0)
            {
                bool keep = true;
                char c = pattern[curPatternIdx];
                switch (c)
                {
                    case '?':
                        curTextIdx--;
                        curPatternIdx--;
                        break;
                    case ']':
                        int sLoc = pattern.Slice(0, curPatternIdx).LastIndexOf('['); // No escaping concept
                        if (sLoc == -1) throw new ApplicationException("Missing start sqbracket");
                        int eLen = curPatternIdx - sLoc - 1; // Now count of elements
                        if (pattern.Slice(sLoc + 1, eLen).IndexOf(text[curTextIdx]) != -1)
                        {
                            curTextIdx--;
                            curPatternIdx -= eLen + 2;
                        }
                        else
                            keep = false;

                        break;
                    default:
                        if (c == text[curTextIdx])
                        {
                            curTextIdx--;
                            curPatternIdx--;
                        }
                        else
                            keep = false;

                        break;
                }

                if (curPatternIdx < 0) return curTextIdx + 1;

                if (keep) continue;

                baseIdx--;
                if (baseIdx < 0) return -1;
                curTextIdx = baseIdx;
                curPatternIdx = pattern.Length - 1;
            }

            return -1;
        }

        private static Func<ReadOnlyMemory<string>, FilterType> F_Any(Func<ReadOnlyMemory<string>, FilterType> after) =>
            path =>
            {
                if (path.Length == 1) return FilterType.Affirm;
                return after.Invoke(path.Slice(1)) == FilterType.Affirm ? FilterType.Affirm : FilterType.NoMatch;
            };

        private static Func<ReadOnlyMemory<string>, FilterType> F_DeepAny(
            Func<ReadOnlyMemory<string>, FilterType> after) => path =>
        {
            for (int i = path.Length - 1; i >= 0; i--)
                if (after.Invoke(path.Slice(i)) == FilterType.Affirm)
                    return FilterType.Affirm;
            return FilterType.NoMatch;
        };

        private static Func<ReadOnlyMemory<string>, FilterType> F_Invert(
            Func<ReadOnlyMemory<string>, FilterType> filter) => path =>
            filter.Invoke(path) switch
            {
                FilterType.Affirm => FilterType.Deny,
                FilterType.Deny => FilterType.Affirm,
                _ => FilterType.NoMatch
            };

        /// <summary>
        /// Filter result type
        /// </summary>
        internal enum FilterType
        {
            /// <summary>
            /// Affirmation from current filter
            /// </summary>
            Affirm,

            /// <summary>
            /// Not matched from current filter
            /// </summary>
            NoMatch,

            /// <summary>
            /// Denial from current filter (should only be for root level filter)
            /// </summary>
            Deny
        }

        /// <summary>
        /// Path filter
        /// </summary>
        public class Filter
        {
            /// <summary>
            /// Path filters
            /// </summary>
            private readonly List<Func<ReadOnlyMemory<string>, FilterType>> _filters;

            internal Filter(List<Func<ReadOnlyMemory<string>, FilterType>> filters)
            {
                _filters = filters;
            }

            /// <summary>
            /// Test relative path for inclusion
            /// </summary>
            /// <param name="path">Path to test</param>
            /// <returns>True if includes</returns>
            public bool Test(string path)
            {
                var split = path.Split('/', '\\');
                return _filters.Aggregate(false, (current, filter) => filter.Invoke(split) switch
                {
                    FilterType.Affirm => true,
                    FilterType.Deny => false,
                    _ => current
                });
            }
        }
    }
}
