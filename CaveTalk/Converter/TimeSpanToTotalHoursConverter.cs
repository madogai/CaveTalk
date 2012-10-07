namespace CaveTube.CaveTalk.Converter {
	using System;
	using System.Windows.Data;

	public sealed class TimeSpanToTotalHoursConverter : IValueConverter {
		#region IValueConverter メンバー

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			if (value is TimeSpan == false) {
				return String.Empty;
			}

			var timeSpan = (TimeSpan)value;
			return String.Format("{0}:{1:d2}:{2:d2}", (timeSpan.Days * 24 + timeSpan.Hours), timeSpan.Minutes, timeSpan.Seconds);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			throw new NotImplementedException();
		}

		#endregion IValueConverter メンバー
	}
}
