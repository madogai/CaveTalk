namespace CaveTube.CaveTalk.Converter {
	using System;
	using System.Windows;
	using System.Windows.Data;

	public sealed class BooleanToVisibilityConverter : IValueConverter {
		#region IValueConverter メンバー

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			if (value is Boolean == false) {
				return Visibility.Collapsed;
			}

			if ((Boolean)value) {
				return Visibility.Visible;
			} else {
				return Visibility.Collapsed;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			throw new NotImplementedException();
		}

		#endregion IValueConverter メンバー
	}
}
