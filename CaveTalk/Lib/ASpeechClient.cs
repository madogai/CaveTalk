namespace CaveTube.CaveTalk.Lib {
	using System;
	using System.Linq;
	using System.Text.RegularExpressions;
	using CaveTube.CaveTalk.Model;

	public abstract class ASpeechClient : IDisposable {
		public static ASpeechClient CreateInstance() {
			var context = new CaveTalkContext();
			var config = context.Config.First();

			context.Entry(config).Reload();

			switch ((SpeakApplicationState)config.SpeakApplication) {
				case SpeakApplicationState.Softalk:
					return new SofTalkClient(config.SofTalkPath);
				default:
					return new BouyomiClientWrapper();
			}
		}

		private CaveTalkContext context;

		public abstract String ApplicationName { get; }

		public virtual Boolean IsConnect { get; private set; }

		protected ASpeechClient() {
			this.context = new CaveTalkContext();
		}

		public virtual Boolean Connect() {
			this.IsConnect = true;
			return true;
		}

		public virtual void Disconnect() {
			this.IsConnect = false;
		}

		public abstract void Dispose();

		protected abstract Boolean Speak(String text);

		public Boolean Speak(Model.Message message) {
			if (this.IsConnect == false) {
				return false;
			}

			var config = this.context.Config.First();

			var comment = message.Comment;

			comment = message.IsAsciiArt ? "アスキーアート" : comment;

			comment = Regex.Replace(comment, @"https?://(?:[^.]+\.)?(?:images-)?amazon\.(?:com|ca|co\.uk|de|co\.jp|jp|fr|cn)(/.+)(?![\w\s!?&.\/\+:;#~%""=-]*>)", "アマゾンリンク");

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