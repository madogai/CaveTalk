namespace CaveTalk.Utils {

	using System;

	public sealed class JavaScriptTime {
		private const String UtcId = "UTC";

		private readonly static DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public static Double ToDouble(DateTime dateTime) {
			return ToDouble(dateTime, UtcId);
		}

		public static Double ToDouble(DateTime dateTime, String timezoneId) {
			var target = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dateTime.ToUniversalTime(), UtcId, timezoneId);
			var timespan = new TimeSpan(target.Ticks - UnixEpoch.Ticks);
			return timespan.TotalMilliseconds;
		}

		public static DateTime ToDateTime(Double javaScriptTime) {
			return ToDateTime(javaScriptTime, UtcId);
		}

		public static DateTime ToDateTime(Double javaScriptTime, String timezoneId) {
			var dateTime = UnixEpoch.AddMilliseconds(javaScriptTime);
			return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dateTime, UtcId, timezoneId);
		}
	}
}