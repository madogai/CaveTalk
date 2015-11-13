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
		private static Int32 MAX_LENGTH = 90;
		private const String textBlockFormat = @"<TextBlock xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">{0}</TextBlock>";

		public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture) {
			var text = value as String;
			if (text == null) {
				return Binding.DoNothing;
			}

			try {
				var escapedText = text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;").Replace("{", "{}{");
				var lineBreakText = escapedText.Replace("\n", "<LineBreak />");
				var autolinkedText = Regex.Replace(lineBreakText, @"(?:http|https|ftp):\/\/[\w\!\?=&,.\/\+:;#~%\-\{\}@]+(?![\w\s\!\?&,.\/\+:;#~%""=\-\{\}@]*>)", m => {
					var abbreviated = m.Value.Length > MAX_LENGTH ? (m.Value.Substring(0, MAX_LENGTH) + "...") : m.Value;
					return String.Format("<Hyperlink NavigateUri=\"{0}\"><Run>{1}</Run><Hyperlink.ToolTip>Loading ...</Hyperlink.ToolTip></Hyperlink>", m.Value, abbreviated);
				}, RegexOptions.Multiline);
				var xaml = String.Format(textBlockFormat, autolinkedText);
				return (TextBlock)XamlReader.Parse(xaml);
			} catch (XamlParseException) {
				return text;
			}
		}

		public object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}

	}
}
