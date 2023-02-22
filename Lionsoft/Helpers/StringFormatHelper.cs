using System.Text.RegularExpressions;

namespace Lionsoft.Helpers
{
    public static class StringFormatHelper
    {
        public static string RemoveHtmlTags(this string text)
        {
            return Regex.Replace(text, "<.*?>", " ").Trim();
        }
    }
}