using System.Linq;

namespace OsmVisualizer.Data.Utils
{
    public static class StringExtensions
    {
        public static string OsmSubstring(this string s, int startIndex, int length)
        {
            return s == null || s.Length < startIndex || s.Length < System.Math.Abs(length) 
                ? "" 
                : s.Substring(startIndex, length > 0 ? length : s.Length - 1 + length);
        }

        private static readonly char[] AllowedCharsInNumber = {'-', ',', '.'};
        
        public static string OnlyNumberChars(this string s)
        {
            return s.ToCharArray()
                .Where(c => c <= '9' && c >= '0' || AllowedCharsInNumber.Contains(c))
                .Aggregate("", (res, c) => res + c);
        }
    }
}