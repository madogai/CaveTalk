namespace CaveTube.CaveTalk.Lib {
	using System;
	using System.IO;
	using System.Text;

	public class FlashCommentGeneratorNotifier {
		/// <summary>
		/// Flashコメントジェネレーター用のdatファイルを上書きします。
		/// </summary>
		/// <param name="filePath"></param>
		/// <param name="message"></param>
		/// <exception cref="IOException">IOException</exception>
		public static void write(String filePath, Message message) {

			var sb = new StringBuilder();
			sb.AppendFormat("NAME={0}_EndName", message.Name).AppendLine();
			sb.AppendFormat("COMMENT={0}_EndComment", message.Comment).AppendLine();
			sb.AppendFormat("RGB={0}_EndRGB", message.ListenerId ?? "0").AppendLine();
			sb.AppendFormat("ANCHOR={0}_EndAnchor", message.Number + 50.0d).AppendLine();
			sb.AppendFormat("CHATNO={0}_EndChatNo", message.Number).AppendLine();
			sb.AppendFormat("CASTERHOST={0}_EndCasterHost", false).AppendLine();
			sb.Append("start:none/now:none/onlive:none/rate:none/d_rate:none/No:none/:none");

			File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
		}
	}
}
