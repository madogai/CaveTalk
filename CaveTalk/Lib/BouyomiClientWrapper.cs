using System;
using System.Runtime.Remoting;
using FNF.Utility;

namespace CaveTube.CaveTalk.Lib {
	public sealed class BouyomiClientWrapper : IReadingApplicationClient {
		#region IReadingApplicationClient メンバー

		public String ApplicationName {
			get {
				return "棒読みちゃん";
			}
		}

		public Boolean IsConnect {
			get {
				try {
					var count = this.client.TalkTaskCount;
					return true;
				} catch (RemotingException) {
					return false;
				}
			}
		}

		private BouyomiChanClient client;

		public BouyomiClientWrapper() {
			this.client = new BouyomiChanClient();
		}

		public Boolean Add(String text) {
			try {
				this.client.AddTalkTask(text);
				return true;
			} catch (RemotingException) {
				return false;
			}
		}

		#endregion

		#region IDisposable メンバー

		public void Dispose() {
			if (this.client != null) {
				this.client.Dispose();
			}
		}

		#endregion
	}
}
