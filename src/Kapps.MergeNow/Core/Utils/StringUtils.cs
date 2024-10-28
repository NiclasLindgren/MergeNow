using System;

namespace MergeNow.Core.Utils
{
    public static class StringUtils
    {
        public static string FindCommonPrefix(string str1, string str2, StringComparison stringComparison)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
            {
                return string.Empty;
            }

            int commonLength = 0;
            while (commonLength < str1.Length &&
                commonLength < str2.Length &&
                string.Equals(str1[commonLength].ToString(), str2[commonLength].ToString(), stringComparison))
            {
                commonLength++;
            }

            return str1.Substring(0, commonLength);
        }

        public static string TakeTillLastChar(string str, char chr)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            int lastCharIndex = str.LastIndexOf(chr);

            return lastCharIndex >= 0
                ? str.Substring(0, lastCharIndex + 1)
                : str;
        }

        public static string PickShortest(string str1, string str2)
        {
            if (str1 == null && str2 == null)
            {
                return null;
            }

            if (str1 == null)
            {
                return str2;
            }

            else if (str2 == null)
            {
                return str1;
            }

            return str1.Length <= str2.Length ? str1 : str2;
        }
    }
}
