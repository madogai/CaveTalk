namespace CaveTube.CaveTalk.Converter {
	using System;
	using System.Windows.Data;

	public sealed class TimeSpanToTotalHoursConverter : IValueConverter {
		#region IValueConverter メンバー

		public Object Convert(Object value, Type targetType, Object parameter, System.Globalization.CultureInfo culture) {
			if (value is TimeSpan == false) {
				return String.Empty;
			}

			var timeSpan = (TimeSpan)value;
			var hour = timeSpan.Days * 24 + timeSpan.Hours;
			var minutes = timeSpan.Minutes;
			var secounds = timeSpan.Seconds;
			return $"{hour}:{minutes:d2}:{secounds:d2}";
		}

		public Object ConvertBack(Object value, Type targetType, Object parameter, System.Globalization.CultureInfo culture) {
			throw new NotImplementedException();
		}

		#endregion IValueConverter メンバー
	}
}
