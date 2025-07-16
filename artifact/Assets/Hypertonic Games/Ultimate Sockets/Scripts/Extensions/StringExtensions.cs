
namespace Hypertonic.Modules.UltimateSockets.Extensions
{
    public static class StringExtensions
    {
        public static string SplitCamelCase(this string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, "(?<!^)([A-Z][a-z]|(?<=[a-z])([A-Z]))", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
        }
    }
}
