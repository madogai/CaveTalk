namespace CaveTube.CaveTubeClient {
	using System;
	using System.Collections.Generic;

	internal static class DateExtends {
		private readonly static DateTime UnixBaseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public static Int64 ToUnixEpoch(this DateTime dateTime) {
			return dateTime.ToUnixEpoch(TimeZoneInfo.Local);
		}

		public static Int64 ToUnixEpoch(this DateTime dateTime, TimeZoneInfo timeZoneInfo) {
			dateTime = TimeZoneInfo.ConvertTimeToUtc(dateTime);
			var timespan = new TimeSpan(dateTime.Ticks - UnixBaseTime.Ticks);
			return Convert.ToInt64(timespan.TotalMilliseconds);
		}

		public static DateTime ToDateTime(this Double unixEpoch) {
			return unixEpoch.ToDateTime(TimeZoneInfo.Local);
		}

		public static DateTime ToDateTime(this Double unixEpoch, TimeZoneInfo timeZoneInfo) {
			var inputDateTime = UnixBaseTime.AddMilliseconds(unixEpoch);
			return TimeZoneInfo.ConvertTimeFromUtc(inputDateTime, timeZoneInfo);
		}
	}
}