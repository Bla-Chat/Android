using System;
using System.Globalization;

namespace BlaChat
{
	public class TimeConverter
	{
		private TimeConverter ()
		{
		}

		public static String Convert(string input, string outputFormat) {
			DateTime parsedDate = Parse (input);
			return parsedDate.ToString (outputFormat);
		}

		public static DateTime Parse(string input) {
			const string pattern = "yyyy-MM-dd HH:mm:ss";
			DateTime parsedDate;
			DateTime.TryParseExact (input, pattern, null, DateTimeStyles.None, out parsedDate);
			return parsedDate;
		}

		public static String AutoConvert(string input) {
			DateTime time = Parse (input);

			TimeSpan dif = DateTime.Today - time;

			if (DateTime.Today.ToString("yyyy-MM-dd").Equals(time.ToString("yyyy-MM-dd"))) {
				return "Heute, " + time.ToString ("HH:mm");
			} else if (DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd").Equals(time.ToString("yyyy-MM-dd"))) {
				return "Gestern, " + time.ToString ("HH:mm");
			} else if (dif < TimeSpan.FromDays(7)) {
				return time.ToString ("dddd, HH:mm");
			} else if (dif < TimeSpan.FromDays(365)) {
				return time.ToString ("dd. MMMM");
			} else {
				return time.ToString ("dd.MM.yyyy");
			}
		}

		public static String AutoConvertDate (string input) {
			DateTime time = Parse (input);

			TimeSpan dif = DateTime.Today - time;

			if (DateTime.Today.ToString("yyyy-MM-dd").Equals(time.ToString("yyyy-MM-dd"))) {
				return "Heute";
			} else if (DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd").Equals(time.ToString("yyyy-MM-dd"))) {
				return "Gestern";
			} else if (dif < TimeSpan.FromDays(7)) {
				return time.ToString ("dddd");
			} else if (dif < TimeSpan.FromDays(365)) {
				return time.ToString ("dd. MMMM");
			} else {
				return time.ToString ("dd.MM.yyyy");
			}
		}
	}
}

