using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HL7.Dotnetcore
{
    public static class MessageHelper
    {
        private static readonly string[] lineSeparators = { "\r\n", "\n\r", "\r", "\n" };

        public static List<string> SplitString(string strStringToSplit, string splitBy, StringSplitOptions splitOptions = StringSplitOptions.None)
        {
            return strStringToSplit.Split(new string[] { splitBy }, splitOptions).ToList();
        }

        public static List<string> SplitString(string strStringToSplit, char chSplitBy, StringSplitOptions splitOptions = StringSplitOptions.None)
        {
            return strStringToSplit.Split(new char[] { chSplitBy }, splitOptions).ToList();
        }

        public static List<string> SplitString(string strStringToSplit, char[] chSplitBy, StringSplitOptions splitOptions = StringSplitOptions.None)
        {
            return strStringToSplit.Split(chSplitBy, splitOptions).ToList();
        }

        public static List<string> SplitMessage(string message)
        {
            return message.Split(lineSeparators, StringSplitOptions.None).Where(m => !string.IsNullOrWhiteSpace(m)).ToList();
        }

        public static string LongDateWithFractionOfSecond(DateTime dt)
        {
            return dt.ToString("yyyyMMddHHmmss.FFFF");
        }

        public static string[] ExtractMessages(string messages)
        {
            var expr = "\x0B(.*?)\x1C\x0D";
            var matches = Regex.Matches(messages, expr, RegexOptions.Singleline);
            var list = new List<string>();

            foreach (Match m in matches)
            {
                list.Add(m.Groups[1].Value);
            }

            return list.ToArray();
        }

        public static DateTime? ParseDateTime(string dateTimeString, bool throwExeption = false)
        {
            return ParseDateTime(dateTimeString, out TimeSpan offset, throwExeption);
        }

        public static DateTime? ParseDateTime(string dateTimeString, out TimeSpan offset, bool throwExeption = false)
        {
            var expr = @"^\s*((?:19|20)[0-9]{2})(?:(1[0-2]|0[1-9])(?:(3[0-1]|[1-2][0-9]|0[1-9])(?:([0-1][0-9]|2[0-3])(?:([0-5][0-9])(?:([0-5][0-9](?:\.[0-9]{1,4})?)?)?)?)?)?)?(?:([+-][0-1][0-9]|[+-]2[0-3])([0-5][0-9]))?\s*$";
            var matches = Regex.Matches(dateTimeString, expr, RegexOptions.Singleline);

            offset = new TimeSpan();

            try
            {
                if (matches.Count != 1)
                    throw new FormatException("Invalid date format");

                var groups = matches[0].Groups;
                int year = int.Parse(groups[1].Value);
                int month = groups[2].Success ? int.Parse(groups[2].Value, CultureInfo.InvariantCulture) : 1;
                int day = groups[3].Success ? int.Parse(groups[3].Value, CultureInfo.InvariantCulture) : 1;
                int hours = groups[4].Success ? int.Parse(groups[4].Value, CultureInfo.InvariantCulture) : 0;
                int mins = groups[5].Success ? int.Parse(groups[5].Value, CultureInfo.InvariantCulture) : 0;

                float fsecs = groups[6].Success ? float.Parse(groups[6].Value, CultureInfo.InvariantCulture) : 0;
                int secs = (int)Math.Truncate(fsecs);
                int msecs = (int)Math.Truncate(fsecs * 1000) % 1000;

                int tzh = groups[7].Success ? int.Parse(groups[7].Value, CultureInfo.InvariantCulture) : 0;
                int tzm = groups[8].Success ? int.Parse(groups[8].Value, CultureInfo.InvariantCulture) : 0;
                offset = new TimeSpan(tzh, tzm, 0);

                return new DateTime(year, month, day, hours, mins, secs, msecs);
            }
            catch
            {
                if (throwExeption)
                    throw;

                return null;
            }
        }

        /// <summary>
        /// Serialize string to MLLP escaped byte array
        /// </summary>
        /// <param name="message">String to serialize</param>
        /// <param name="encoding">Text encoder (optional)</param>
        /// <returns>MLLP escaped byte array</returns>
        public static byte[] GetMLLP(string message, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            
            byte[] data = encoding.GetBytes(message);
            byte[] buffer = new byte[data.Length + 3];
            buffer[0] = 11; // VT

            Array.Copy(data, 0, buffer, 1, data.Length);
            buffer[buffer.Length - 2] = 28; // FS
            buffer[buffer.Length - 1] = 13; // CR

            return buffer;
        }
    }
}
