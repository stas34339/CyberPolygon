using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace CyberPolygon.Services
{
    public static class AnswerValidator
    {
        public static bool Validate(string expected, string actual)
        {
            if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(actual))
                return false;

            var listSeparators = new[] { ',', ';' };
            var expectedItems = expected.Split(listSeparators, StringSplitOptions.RemoveEmptyEntries);
            var actualItems = actual.Split(listSeparators, StringSplitOptions.RemoveEmptyEntries);

            if (expectedItems.Length > 1 || actualItems.Length > 1)
            {
                if (expectedItems.Length != actualItems.Length) return false;

                var normExpected = expectedItems.Select(NormalizeItem).OrderBy(x => x).ToList();
                var normActual = actualItems.Select(NormalizeItem).OrderBy(x => x).ToList();

                for (int i = 0; i < normExpected.Count; i++)
                {
                    if (!CompareSingleItem(normExpected[i], normActual[i]))
                        return false;
                }
                return true;
            }

            return CompareSingleItem(NormalizeItem(expected), NormalizeItem(actual));
        }

        private static string NormalizeItem(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            string result = input.Trim().ToLowerInvariant();
            result = result.Replace("\\", "/");

            if ((result.Contains(":") || result.Contains("-")) && !result.Contains("/"))
            {
                var stripped = result.Replace(":", "").Replace("-", "").Replace(".", "");
                if (stripped.Length == 12) result = stripped;
            }

            if (!result.Contains(".") && result.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length == 4)
            {
                result = string.Join(".", result.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            }

            result = result.Replace("—", "-").Replace("–", "-");
            if (Regex.IsMatch(result, @"^\d{2}\.\d{2}(\.\d{2})?$"))
            {
                result = result.Replace(".", ":");
            }

            result = result.Replace(" ", "");
            return result;
        }

        private static bool CompareSingleItem(string expected, string actual)
        {
            if (expected == actual) return true;

            int distance = CalculateLevenshteinDistance(expected, actual);
            int allowedTolerance = (int)Math.Floor(expected.Length * 0.1);

            if (expected.Length < 10) allowedTolerance = 0;

            return distance <= allowedTolerance;
        }

        private static int CalculateLevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
            if (string.IsNullOrEmpty(t)) return s.Length;

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }
    }
}