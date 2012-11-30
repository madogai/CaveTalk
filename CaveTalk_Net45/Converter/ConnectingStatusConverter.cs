namespace CaveTube.CaveTalk.Converter {
	using System;
	using System.Windows.Data;

	public sealed class ConnectingStatusConverter : IValueConverter {
		#region IValueConverter メンバー

		public Object Convert(Object value, Type targetType, Object parameter, System.Globalization.CultureInfo culture) {
			if (value is Boolean == false) {
				return String.Empty;
			}

			var isConnect = (Boolean)value;
			var text = isConnect ? "ON" : "OFF";

			return text;
		}

		public Object ConvertBack(Object value, Type targetType, Object parameter, System.Globalization.CultureInfo culture) {
			throw new NotImplementedException();
		}

		#endregion IValueConverter メンバー
	}
}
