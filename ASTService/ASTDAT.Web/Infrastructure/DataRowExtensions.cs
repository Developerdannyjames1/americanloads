using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web;

namespace ASTDAT.Web.Infrastructure
{
    public static class DataRowExtensions
    {
        public static string GetCell(this DataRowCollection rows, int row, int col)
        {
            if (row > rows.Count)
            {
                return "";
            }

            if (col > rows[row].Table.Columns.Count)
            {
                return "";
            }

            return $"{rows[row][col]}";
        }

        public static string GetCell(this DataRowCollection rows, string coordinates)
        {
            var columns = new Dictionary<string, int> {
                { "AA", 26},
                { "AB", 27},
                { "AC", 28},
                { "AD", 29},
                { "AE", 30},
                { "AF", 31},
                { "AG", 32},
                { "AH", 33},
                { "AI", 34},
                { "AJ", 35},
                { "AK", 36},
                { "AL", 37},
                { "AM", 38},
            };

            string value = null;

            if (columns.ContainsKey(coordinates.Substring(0, 2).ToUpper()))
            {
                var row = int.Parse(coordinates.Substring(2, coordinates.Length - 2)) - 1;

                value = rows[row][columns[coordinates.Substring(0, 2)]].ToString();
            }
            else
            {
                var row = int.Parse(coordinates.Substring(1, coordinates.Length - 1)) - 1;
                var column = char.ToUpper(coordinates[0]) - 65;

                value = rows[row][column].ToString();


            }
            return value;
        }
        public static bool TryGetCell(this DataRowCollection rows, string coordinates, out string value)
        {
            value = string.Empty;
            bool retval = false;

            try
            {
                value = GetCell(rows, coordinates);
                retval = true;
            }
            catch (Exception)
            {
                retval = false;
            }

            return retval;
        }
        public static string Truncate(this string value, int lenght)
        {
            // Valencio 12-29-16 If Value is null, then return null
            if (value == null)
                return null;

            if (value.Length > lenght)
                value = value.Substring(0, lenght);

            return value;
        }

        public static DateTime? ToNullableDateTime(this string value)
        {
            //try
            //{
            //    return DateTime.FromOADate(double.Parse(value));
            //}
            //catch
            //{
            //    return null;
            //}
            DateTime result;

            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (!DateTime.TryParse(value, out result))
                return null;

            return result;
        }

        public static short? ToNullableShort(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return short.Parse(value);
        }

        public static decimal? ToNullableDecimal(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            decimal result;
            if (decimal.TryParse(value.TrimEnd('"').Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            return null;
        }
        public static decimal? ToNullableDecimal(this string value, int round)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            decimal result;
            if (decimal.TryParse(value.TrimEnd('"').Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                return Math.Round(result, round);
            }

            return null;
        }
        public static decimal? ToNullableDecimalFromPercent(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            decimal result;
            var percent = value.TrimEnd('"').Replace(",", ".");
            percent = percent.Substring(0, percent.Length - 1);
            if (decimal.TryParse(percent, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            return null;
        }

        public static int? ToNullableInt(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            int result;
            if (!int.TryParse(value, out result))
                return null;

            return result;
        }

        public static byte? ToNullableByte(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return byte.Parse(value);
        }

        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    action(item);
                }
            }
        }

        public static string Between(this string input, string findFrom, string findTo, int startFrom = 0)
        {
            //Get a substring from the specified starting position
            input = input.Substring(startFrom);

            //Find the starting phrase
            var start = input.IndexOf(findFrom, StringComparison.Ordinal);
            if (start == -1)
            {
                return string.Empty;
            }

            //Find the ending phrase
            var to = input.IndexOf(findTo, start + findFrom.Length, StringComparison.Ordinal);
            if (to == -1)
            {
                to = input.Length;
            }

            //Get the text between the two phrases
            return input.Substring(start + findFrom.Length, to - start - findFrom.Length);
        }
    }
}