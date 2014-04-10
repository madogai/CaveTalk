namespace CaveTube.CaveTalk.Converter {
	using System;
	using System.Windows;
	using System.Windows.Data;

	public sealed class BooleanToVisibilityConverter : IValueConverter {
		#region IValueConverter メンバー

		public Object Convert(Object value, Type targetType, Object parameter, System.Globalization.CultureInfo culture) {
			if (value is Boolean == false) {
				return Visibility.Collapsed;
			}

			if ((Boolean)value) {
				return Visibility.Visible;
			} else {
				return Visibility.Collapsed;
			}
		}

		public Object ConvertBack(Object value, Type targetType, Object parameter, System.Globalization.CultureInfo culture) {
			throw new NotImplementedException();
		}

		#endregion IValueConverter メンバー
	}
}
