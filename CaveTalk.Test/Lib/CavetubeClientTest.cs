namespace CaveTube.CaveTalk.Lib.Test {

	using System;
	using CaveTube.CaveTalk;
	using CaveTube.CaveTalk.Utils;
	using Codeplex.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using SocketIO;

	[TestClass()]
	public class CavetubeClientTest {
		private CavetubeClient_Accessor target = new CavetubeClient_Accessor();

		[TestInitialize()]
		public void MyTestInitialize() {
			var socketIOClient = new MoqSocketIO();
			this.target = new CavetubeClient_Accessor(socketIOClient);
		}

		[TestMethod]
		public void OnMessage_モードがpost() {
			// arrange
			Summary actSummary = null;
			Message actMessage = null;

			target.add_OnMessage((obj, summary, message) => {
				actSummary = summary;
				actMessage = message;
			});

			var expSummary = new Summary(1, 1);
			var expMessage = new Message(1, "hoge", "comment", new DateTime(2000, 1, 1), false, false);

			// act
			var client = (MoqSocketIO)this.target.client;
			client.TriggerOnMessage(new {
				ret = true,
				mode = "post",
				listener = expSummary.Listener,
				viewer = expSummary.PageView,
				comment_num = expMessage.Number,
				name = expMessage.Name,
				message = expMessage.Comment,
				time = JavaScriptTime.ToDouble(expMessage.Time, TimeZoneKind.Japan),
				auth = false,
				is_ban = false,
			});

			// assert
			Assert.AreEqual(expSummary, actSummary);
			Assert.AreEqual(expMessage, actMessage);
		}

		[TestMethod]
		public void OnMessage_モードがjoin() {
			Int32 actListener = 0;

			// arrange
			target.add_OnUpdateMember((obj, listener) => {
				actListener = listener;
			});

			var expListener = 1;

			// act
			var client = (MoqSocketIO)this.target.client;
			client.TriggerOnMessage(new {
				ret = true,
				mode = "leave",
				ipcount = expListener,
			});

			// assert
			Assert.AreEqual(expListener, actListener);
		}

		[TestMethod]
		public void OnMessage_モードがleave() {
			Int32 actListener = 0;

			// arrange
			target.add_OnUpdateMember((obj, listener) => {
				actListener = listener;
			});

			var expListener = 1;

			// act
			var client = (MoqSocketIO)this.target.client;
			client.TriggerOnMessage(new {
				ret = true,
				mode = "join",
				ipcount = expListener,
			});

			// assert
			Assert.AreEqual(expListener, actListener);
		}

		[TestMethod]
		public void OnMessage_retがfalseの時() {
			Int32 actListener = 0;

			// arrange
			target.add_OnUpdateMember((obj, listener) => {
				actListener = listener;
			});

			var expListener = 0;

			// act
			var client = (MoqSocketIO)this.target.client;
			client.TriggerOnMessage(new {
				ret = false,
				mode = "join",
				ipcount = 1,
			});

			// assert
			Assert.AreEqual(expListener, actListener);
		}

		private sealed class MoqSocketIO : ISocketIOClient {
			private Boolean isConnect;

			#region ISocketIOClient メンバー

			public event Action<object, EventArgs> OnOpen;
			public event Action<object, string> OnMessage;
			public event Action<object, string> OnError;
			public event Action<object, EventArgs> OnClose;

			public bool IsConnect {
				get { return isConnect; }
			}

			public void Connect() {
				this.isConnect = true;
			}

			public void Close() {
				this.isConnect = false;
			}

			public void Send(string message) {
			}

			#endregion ISocketIOClient メンバー

			#region IDisposable メンバー

			public void Dispose() {
				this.Close();
			}

			#endregion IDisposable メンバー

			public void TriggerOnMessage(Object obj) {
				var message = DynamicJson.Serialize(obj);
				this.OnMessage(null, message);
			}
		}
	}
}