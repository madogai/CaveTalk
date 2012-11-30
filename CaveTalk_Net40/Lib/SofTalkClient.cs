namespace CaveTube.CaveTalk.Lib {
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;

	public sealed class SofTalkClient : ASpeechClient {
		private String exePath;
		private Int32 taskCount;

		public SofTalkClient(String exePath) {
			this.exePath = exePath;
		}

		#region ASpeechClient メンバー

		public override String ApplicationName {
			get {
				return "SofTalk";
			}
		}

		public override Boolean IsConnect {
			get { return base.IsConnect && this.CanSpeech(); }
		}

		private Boolean CanSpeech() {
			return File.Exists(this.exePath);
		}

		public override Boolean Connect() {
			if (this.CanSpeech() == false) {
				return false;
			}

			// アプリケーションをスムーズに実行するため、あらかじめ起動しておきます。
			var process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = this.exePath,
				},
			};
			process.Start();
			base.Connect();
			return true;
		}

		public override Boolean Speak(String text) {
			if (this.IsConnect == false) {
				return false;
			}

			this.taskCount += 1;

			var process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = this.exePath,
					Arguments = String.Format("/W:{0}", text),
				},
				EnableRaisingEvents = true,
			};
			process.Exited += (sender, e) => {
				this.taskCount -= 1;
			};
			process.Start();
			return true;
		}

		public sealed override void Dispose() {
			var ps = Process.GetProcessesByName("softalk");
			ps.ForEach(p => p.CloseMainWindow());
		}

		#endregion
	}
}
