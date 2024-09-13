using System.Globalization;

namespace MCPhase3.CodeRepository
{
    public static class StringExt
    {
        public static bool IsNumeric(this string text) => double.TryParse(text, CultureInfo.CurrentCulture, out _);

    }
}
