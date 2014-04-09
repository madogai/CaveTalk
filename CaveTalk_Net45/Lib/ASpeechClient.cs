namespace CaveTube.CaveTalk.Lib {
	using System;
	using System.Linq;
	using System.Text.RegularExpressions;
	using CaveTube.CaveTalk.Model;

	public abstract class ASpeechClient : IDisposable {
		public static ASpeechClient CreateInstance() {
			var config = Config.GetConfig();

			switch (config.SpeakApplication) {
				case Config.SpeakApplicationType.Bouyomichan:
					return new BouyomiClientWrapper();
				case Config.SpeakApplicationType.SofTalk:
					return new SofTalkClient(config.SofTalkPath);
				case Config.SpeakApplicationType.UserSound:
					return new UserSoundClient();
				default:
					return new NoSoundClient();
			}
		}

		public abstract String ApplicationName { get; }

		public virtual Boolean IsConnect { get; private set; }

		public virtual Boolean Connect() {
			this.IsConnect = true;
			return true;
		}

		public virtual void Disconnect() {
			this.IsConnect = false;
		}

		public abstract void Dispose();

		public abstract Boolean Speak(String text);

		public Boolean Speak(Message message) {
			if (this.IsConnect == false) {
				return false;
			}

			var config = Config.GetConfig();

			var comment = message.Comment;

			comment = message.IsAsciiArt ? "アスキーアート" : comment;

			// AmazonURLの省略
			comment = Regex.Replace(comment, @"https?://(?:[^.]+\.)?(?:images-)?amazon\.(?:com|ca|co\.uk|de|co\.jp|jp|fr|cn)(/.+)(?![\w\s!?&.\/\+:;#~%""=-]*>)", "アマゾンリンク");

			// URLの省略
			comment = Regex.Replace(comment, @"((http|https|ftp)://[\w!?=&,./\+:;#~%-]+(?![\w\s!?&,./\+:;#~%""=-]*>))", "URL省略");

			comment = comment.Replace("\n", " ");

			if (config.ReadCommentName && String.IsNullOrWhiteSpace(message.Name) == false) {
				comment = String.Format("{0}さん {1}", message.Name, comment);
			}

			if (config.ReadCommentNumber) {
				comment = String.Format("コメント{0} {1}", message.Number, comment);
			}

			return this.Speak(comment);
		}
	}
}