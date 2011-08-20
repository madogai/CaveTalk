namespace SocketIOClient.Test {
	using SocketIO;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using System;

	[TestClass()]
	public class SocketIOClientTest {
		private SocketIOClient_Accessor target;

		[TestInitialize()]
		public void MyTestInitialize() {
			Func<Uri, String, ITransport> clientBuilder = (uri, sessionId) => {
				return new MoqTransport();
			};

			this.target = new SocketIOClient_Accessor(new Uri("http://www.google.com"), clientBuilder);
		}

		[TestMethod]
		public void OnMessage_ハンドシェイク() {
			// arrange

			// act
			var client = (MoqTransport)this.target.client;
			client.TriggerOnMessage("2::");
			var actual = client.SendMessage;

			// assert
			Assert.AreEqual("2::", actual);
		}

		[TestMethod]
		public void OnMessage_メッセージ_IDとエンドポイントなし() {
			String actual = "";

			// arrange
			this.target.add_OnMessage((obj, msg) => {
				actual = msg;
			});

			// act
			var client = (MoqTransport)this.target.client;
			client.TriggerOnMessage("3:::hoge");

			// assert
			Assert.AreEqual("hoge", actual);
		}

		[TestMethod]
		public void OnMessage_メッセージ() {
			String actual = "";

			// arrange
			this.target.add_OnMessage((obj, msg) => {
				actual = msg;
			});

			// act
			var client = (MoqTransport)this.target.client;
			client.TriggerOnMessage("3:id:endpoint:hoge");

			// assert
			Assert.AreEqual("hoge", actual);
		}

		[TestMethod]
		public void Close_Disconnect確認() {
			// arrange

			// act
			var client = (MoqTransport)this.target.client;
			this.target.Close();
			var actual = client.SendMessage;

			// assert
			Assert.AreEqual("0::", actual);
		}

		private class MoqTransport : ITransport {
			public String SendMessage { get; set; }

			#region ITransport メンバー

			public event Action<object, EventArgs> OnOpen;

			public event Action<object, string> OnMessage;

			public event Action<object, string> OnError;

			public event Action<object, EventArgs> OnClose;

			public bool IsConnect {
				get { return true; }
			}

			public void Connect() {
			}

			public void Close() {
			}

			public void Send(string message) {
				this.SendMessage = message;
			}

			#endregion

			#region IDisposable メンバー

			public void Dispose() {
			}

			public void TriggerOnMessage(String message) {
				this.OnMessage(null, message);
			}

			#endregion
		}
	}
}
