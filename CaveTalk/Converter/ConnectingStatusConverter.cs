namespace CaveTube.CaveTalk.Converter {
	using System;
	using System.Windows.Data;

	public sealed class ConnectingStatusConverter : IValueConverter {
		#region IValueConverter メンバー

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			if (value is Boolean == false) {
				return String.Empty;
			}

			var isConnect = (Boolean)value;
			var text = isConnect ? "ON" : "OFF";

			return text;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			throw new NotImplementedException();
		}

		#endregion IValueConverter メンバー
	}
}
