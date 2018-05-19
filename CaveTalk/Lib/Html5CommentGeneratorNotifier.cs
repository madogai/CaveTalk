namespace CaveTube.CaveTalk.Lib {
	using System;
	using System.Xml.Linq;

	public class Html5CommentGeneratorNotifier {
		private static readonly DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>
		/// HTML5コメントジェネレーター用のxmlファイルを上書きします。
		/// </summary>
		/// <param name="filePath"></param>
		/// <param name="message"></param>
		/// <exception cref="IOException">IOException</exception>
		public static void write(String filePath, Message message) {
			var doc = XDocument.Load(filePath);

			var comment = new XElement("comment");
			comment.SetAttributeValue("service", "cavetube");
			comment.SetAttributeValue("time", ToUnixTime(message.PostTime));
			comment.SetAttributeValue("no", message.Number);
			comment.Value = message.Comment;

			doc.Root.RemoveNodes();
			doc.Root.Add(comment);

			doc.Save(filePath);
		}

		public static Int64 ToUnixTime(DateTime targetTime) {
			TimeSpan elapsedTime = targetTime.ToUniversalTime() - UNIX_EPOCH;
			return (Int64)elapsedTime.TotalSeconds;
		}
	}
}
