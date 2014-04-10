namespace CaveTube.CaveTalk.Converter {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Windows.Data;
	using System.Windows;

	public class EnumBooleanConverter : IValueConverter {
		#region IValueConverter Members
		public Object Convert(Object value, Type targetType, Object parameter, System.Globalization.CultureInfo culture) {
			var parameterString = parameter as String;
			if (parameterString == null) {
				return DependencyProperty.UnsetValue;
			}

			if (Enum.IsDefined(value.GetType(), value) == false) {
				return DependencyProperty.UnsetValue;
			}

			var parameterValue = Enum.Parse(value.GetType(), parameterString);

			return parameterValue.Equals(value);
		}

		public Object ConvertBack(Object value, Type targetType, Object parameter, System.Globalization.CultureInfo culture) {
			var parameterString = parameter as String;
			if (parameterString == null) {
				return DependencyProperty.UnsetValue;
			}

			return Enum.Parse(targetType, parameterString);
		}
		#endregion
	}

}
