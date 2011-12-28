namespace CaveTube.CaveTalk.Converter {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Windows.Data;
	using System.Globalization;
	using System.Windows.Controls;
	using System.Windows.Markup;
	using System.Text.RegularExpressions;

	public class AutoLinkConverter : IValueConverter {
		private const String textBlockFormat = @"<TextBlock xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" TextWrapping=""Wrap"">{0}</TextBlock>";

		public object Convert(Object value, Type targetType, Object parameter, CultureInfo culture) {
			var text = value as String;
			if (text == null) {
				return Binding.DoNothing;
			}

			var escapedText = text.Replace("<", "&lt;").Replace(">", "&gt;").Replace("&", "&amp;").Replace("\"", "&quot;").Replace("'", "&apos;").Replace("{", "{}{");
			var autolinkedText = Regex.Replace(escapedText, @"https?://[-_.!~*'()a-zA-Z0-9;/?:@&=+$,%#]+", @"<Hyperlink NavigateUri=""$&""><Run Text=""$&"" /></Hyperlink>", RegexOptions.Multiline);
			var xaml = String.Format(textBlockFormat, autolinkedText);
			return (TextBlock)XamlReader.Parse(xaml);
		}

		public object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}

	}
}
