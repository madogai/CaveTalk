namespace CaveTalk.Utils {

	using System;
using System.Collections.Generic;

	public sealed class JavaScriptTime {
		private readonly static DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		private readonly static IDictionary<TimeZoneKind, String> TimeZoneKindDictionary = new Dictionary<TimeZoneKind, String> {
			{ TimeZoneKind.Utc, "UTC" },
			{ TimeZoneKind.Japan, "Tokyo Standard Time"},
		};

		public static Double ToDouble(DateTime dateTime) {
			return ToDouble(dateTime, TimeZoneKind.Utc);
		}

		public static Double ToDouble(DateTime dateTime, TimeZoneKind timezoneKind) {
			var timezoneInfo = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneKindDictionary[timezoneKind]);
			var target = dateTime.AddTicks(-1 * timezoneInfo.BaseUtcOffset.Ticks);
			var timespan = new TimeSpan(target.Ticks - UnixEpoch.Ticks);
			return timespan.TotalMilliseconds;
		}

		public static DateTime ToDateTime(Double javaScriptTime) {
			return ToDateTime(javaScriptTime, TimeZoneKind.Utc);
		}

		public static DateTime ToDateTime(Double javaScriptTime, TimeZoneKind timezoneKind) {
			var timezoneInfo = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneKindDictionary[timezoneKind]);
			var inputDateTime = UnixEpoch.AddMilliseconds(javaScriptTime);
			var convertDateTime = inputDateTime.AddTicks(timezoneInfo.BaseUtcOffset.Ticks);
			return convertDateTime;
		}
	}

	public enum TimeZoneKind {
		Utc, Japan,
	}
}