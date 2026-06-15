using System;

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

        int n = s.Length;
        int m = t.Length;

        if (n == 0) return m;
        if (m == 0) return n;

        int[] p = new int[n + 1];
        int[] d = new int[n + 1];

        for (int i = 0; i <= n; i++)
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

            int[] temp = p;
            p = d;
            d = temp;
        }

        return p[n];
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

        string s = source.Trim().ToLowerInvariant();
        string t = target.Trim().ToLowerInvariant();

        if (s == t)
        {
            distance = 0;
            return true;
        }

        distance = Calculate(s, t);
        int maxLen = Math.Max(s.Length, t.Length);

        if (maxLen <= 5)
        {
            return distance <= 2;
        }
        else
        {
            int threshold = (int)Math.Floor(maxLen * 0.20);
            return distance <= threshold;
        }
    }
}
