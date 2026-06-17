using System;
using System.Buffers;

namespace FijiPayroll.Shared.Utilities;

/// <summary>
/// Utility class to calculate Levenshtein Distance and verify fuzzy matches.
/// </summary>
public static class LevenshteinDistance
{
    /// <summary>
    /// Computes the Levenshtein distance between two strings using an optimized two-row algorithm.
    /// </summary>
    /// <param name="s">First string.</param>
    /// <param name="t">Second string.</param>
    /// <returns>The Levenshtein distance as an integer.</returns>
    public static int Calculate(string s, string t)
    {
        if (s == null) throw new ArgumentNullException(nameof(s));
        if (t == null) throw new ArgumentNullException(nameof(t));

        return Calculate(s.AsSpan(), t.AsSpan());
    }

    /// <summary>
    /// Computes the Levenshtein distance between two character spans.
    /// </summary>
    private static int Calculate(ReadOnlySpan<char> s, ReadOnlySpan<char> t)
    {
        int n = s.Length;
        int m = t.Length;

        if (n == 0) return m;
        if (m == 0) return n;

        int length = n + 1;
        int[]? rentedP = null;
        int[]? rentedD = null;
        Span<int> p = length <= 256 ? stackalloc int[length] : (rentedP = ArrayPool<int>.Shared.Rent(length));
        Span<int> d = length <= 256 ? stackalloc int[length] : (rentedD = ArrayPool<int>.Shared.Rent(length));

        try
        {
            for (int i = 0; i < length; i++)
            {
                p[i] = i;
            }

            for (int j = 1; j <= m; j++)
            {
                char tj = t[j - 1];
                d[0] = j;

                for (int i = 1; i <= n; i++)
                {
                    int cost = (s[i - 1] == tj) ? 0 : 1;
                    d[i] = Math.Min(Math.Min(d[i - 1] + 1, p[i] + 1), p[i - 1] + cost);
                }

                Span<int> temp = p;
                p = d;
                d = temp;
            }

            return p[n];
        }
        finally
        {
            if (rentedP != null) ArrayPool<int>.Shared.Return(rentedP);
            if (rentedD != null) ArrayPool<int>.Shared.Return(rentedD);
        }
    }

    /// <summary>
    /// Computes the Levenshtein distance between two character spans case-insensitively.
    /// </summary>
    private static int CalculateCaseInsensitive(ReadOnlySpan<char> s, ReadOnlySpan<char> t)
    {
        int n = s.Length;
        int m = t.Length;

        if (n == 0) return m;
        if (m == 0) return n;

        int length = n + 1;
        int[]? rentedP = null;
        int[]? rentedD = null;
        Span<int> p = length <= 256 ? stackalloc int[length] : (rentedP = ArrayPool<int>.Shared.Rent(length));
        Span<int> d = length <= 256 ? stackalloc int[length] : (rentedD = ArrayPool<int>.Shared.Rent(length));

        try
        {
            for (int i = 0; i < length; i++)
            {
                p[i] = i;
            }

            for (int j = 1; j <= m; j++)
            {
                char tj = char.ToLowerInvariant(t[j - 1]);
                d[0] = j;

                for (int i = 1; i <= n; i++)
                {
                    int cost = (char.ToLowerInvariant(s[i - 1]) == tj) ? 0 : 1;
                    d[i] = Math.Min(Math.Min(d[i - 1] + 1, p[i] + 1), p[i - 1] + cost);
                }

                Span<int> temp = p;
                p = d;
                d = temp;
            }

            return p[n];
        }
        finally
        {
            if (rentedP != null) ArrayPool<int>.Shared.Return(rentedP);
            if (rentedD != null) ArrayPool<int>.Shared.Return(rentedD);
        }
    }

    /// <summary>
    /// Determines whether the search target matches the search source fuzzily.
    /// Rules:
    /// - Max distance of 2 for short strings (length &lt;= 5)
    /// - 20% of length for longer strings (length &gt; 5)
    /// </summary>
    /// <param name="source">The query string or original source text.</param>
    /// <param name="target">The candidate target string to test match against.</param>
    /// <param name="distance">The calculated Levenshtein distance between strings.</param>
    /// <returns>True if the distance satisfies the threshold rules; otherwise, false.</returns>
    public static bool IsFuzzyMatch(string source, string target, out int distance)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (target == null) throw new ArgumentNullException(nameof(target));

        ReadOnlySpan<char> s = source.AsSpan().Trim();
        ReadOnlySpan<char> t = target.AsSpan().Trim();

        if (s.Equals(t, StringComparison.OrdinalIgnoreCase))
        {
            distance = 0;
            return true;
        }

        int maxLen = Math.Max(s.Length, t.Length);
        int allowedDistance = maxLen <= 5 ? 2 : (int)Math.Floor(maxLen * 0.20);

        int lenDiff = Math.Abs(s.Length - t.Length);
        if (lenDiff > allowedDistance)
        {
            distance = lenDiff;
            return false;
        }

        distance = CalculateCaseInsensitive(s, t);
        return distance <= allowedDistance;
    }
}
