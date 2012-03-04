using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using CaveTube.CaveTalk.Lib;
using CaveTube.CaveTalk.Model;
using CaveTube.CaveTalk.Properties;

namespace CaveTube.CaveTalk.Logic {
	internal sealed class SpeechLogic : IDisposable {
		private CaveTalkContext context;
		private Config config;

		private IReadingApplicationClient client;
		public Boolean SpeechStatus;

		public SpeechLogic() {
			this.context = new CaveTalkContext();
			this.config = this.context.Config.First();
		}

		/// <summary>
		/// 読み上げソフトに接続します。
		/// </summary>
		public void Connect() {
			this.Disconnect();

			this.context.Entry(this.config).Reload();

			switch ((SpeakApplicationState)this.config.SpeakApplication) {
				case SpeakApplicationState.Softalk:
					try {
						this.client = new SofTalkClient(this.config.SofTalkPath);
						this.SpeechStatus = true;
					} catch (FileNotFoundException) {
						MessageBox.Show("SofTalkに接続できませんでした。\nオプションでSofTalk.exeの正しいパスを指定してください。");
						this.SpeechStatus = false;
					}
					break;
				default:
					this.client = new BouyomiClientWrapper();
					if (this.client.IsConnect) {
						this.SpeechStatus = true;
					} else {
						MessageBox.Show("棒読みちゃんに接続できませんでした。\n後から棒読みちゃんを起動した場合は、リボンの読み上げアイコンから読み上げソフトに接続を選択してください。");
						this.SpeechStatus = false;
					}
					break;
			}
		}

		/// <summary>
		/// 読み上げソフトから切断します。
		/// </summary>
		public void Disconnect() {
			if (this.client != null) {
				this.client.Dispose();
				this.SpeechStatus = false;
			}
		}

		/// <summary>
		/// 読み上げソフトにコメントを渡します。
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public Boolean Speak(Model.Message message) {
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

			return this.client.Add(comment);
		}

		public void Dispose() {
			if (this.client == null) {
				return;
			}

			this.client.Dispose();
		}
	}
}
