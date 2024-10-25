namespace MergeNow.Core.Utils
{
    public static class StringUtils
    {
        public static string FindCommonPrefix(string str1, string str2)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
            {
                return string.Empty;
            }

            int commonLength = 0;
            while (commonLength < str1.Length && commonLength < str2.Length && str1[commonLength] == str2[commonLength])
            {
                commonLength++;
            }

            return str1.Substring(0, commonLength);
        }
    }
}
