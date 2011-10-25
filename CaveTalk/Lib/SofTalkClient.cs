using System;
using System.Diagnostics;
using System.IO;

namespace CaveTube.CaveTalk.Lib {
	public sealed class SofTalkClient : IReadingApplicationClient {
		private String exePath;
		private Int32 taskCount;

		public SofTalkClient(String exePath) {
			if (File.Exists(exePath) == false) {
				throw new FileNotFoundException("指定されたファイルが存在しません。");
			}

			this.exePath = exePath;
			var process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = exePath,
				},
			};
			process.Start();
		}

		#region IReadingApplicationClient メンバー

		public String ApplicationName {
			get {
				return "SofTalk";
			}
		}

		public bool IsConnect {
			get { return true; }
		}

		public Boolean Add(string text) {
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

		public void Dispose() {

		}

		#endregion
	}
}
