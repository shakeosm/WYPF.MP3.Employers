using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MCPhase3.CodeRepository
{
    public static class HelperExtension
    {
        public static bool IsNumeric(this string text) => double.TryParse(text, CultureInfo.CurrentCulture, out _);

        /// <summary>This will evaluate a string and check if it has ANY contents in it.</summary>
        /// <param name="text">Text value to parse</param>
        /// <returns>TRUE if there is NO value</returns>
        public static bool IsEmpty(this string text) => string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text);
        
        /// <summary>This will evaluate a string and check if it has any contents in it.</summary>
        /// <param name="text">Text value to parse</param>
        /// <returns>FALSE if there is ANY value</returns>
        public static bool NotEmpty(this string text) => !string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(text);

        /// <summary>This will evaluate a IEnumerable list and check if it has any contents in it.</summary>
        /// <param name="text">List item to parse</param>
        /// <returns>TRUE if there is ANY value</returns>
        public static bool HasItems<T>(this IEnumerable<T> source)
        {
            return (source?.Any() ?? false);
        }
    }

}
