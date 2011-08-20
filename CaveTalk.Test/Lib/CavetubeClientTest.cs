namespace CaveTube.CaveTalk.Lib.Test {

	using System;
	using System.Linq;
	using CaveTube.CaveTalk;
	using CaveTube.CaveTalk.Utils;
	using Codeplex.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using SocketIO;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using CaveTube.CaveTalk.CaveTubeClient;

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

			target.add_OnMessage((summary, message) => {
				actSummary = summary;
				actMessage = message;
			});

			var expSummary = new Summary(DynamicJson.Serialize(new {
				room = "room",
				listener = 1,
				viewer = 1 
			}));
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
			target.add_OnUpdateMember(listener => {
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
			target.add_OnUpdateMember(listener => {
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
			target.add_OnUpdateMember(listener => {
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

		[TestMethod]
		public void Login_成功() {
			// arrange

			// act
			var actual = target.Login("CaveTalk", "cavetalk", "C9A8B58F00CB4E88A8C884CD9C19B868");

			// assert
			Assert.IsFalse(String.IsNullOrEmpty(actual));
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Login_ユーザー名なし() {
			// arrange

			// act
			target.Login(String.Empty, "cavetalk", "C9A8B58F00CB4E88A8C884CD9C19B868");

			// assert
			Assert.Fail("例外が発生しませんでした。");
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Login_パスワードなし() {
			// arrange

			// act
			var actual = target.Login("CaveTalk", String.Empty, "C9A8B58F00CB4E88A8C884CD9C19B868");

			// assert
			Assert.Fail("例外が発生しませんでした。");
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Login_開発者キーなし() {
			// arrange

			// act
			var actual = target.Login("CaveTalk", "cavetalk", String.Empty);

			// assert
			Assert.Fail("例外が発生しませんでした。");
		}

		[TestMethod]
		public void Logout_成功() {
			// arrange

			// act
			var actual = target.Logout("CaveTalk", "cavetalk", "C9A8B58F00CB4E88A8C884CD9C19B868");

			// assert
			Assert.AreEqual(true, actual);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Logout_ユーザー名なし() {
			// arrange

			// act
			var actual = target.Logout(String.Empty, "cavetalk", "C9A8B58F00CB4E88A8C884CD9C19B868");

			// assert
			Assert.Fail("例外が発生しませんでした。");
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Logout_パスワードなし() {
			// arrange

			// act
			var actual = target.Logout("CaveTalk", String.Empty, "C9A8B58F00CB4E88A8C884CD9C19B868");

			// assert
			Assert.Fail("例外が発生しませんでした。");
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Logout_開発者キーなし() {
			// arrange

			// act
			var actual = target.Logout("CaveTalk", "cavetalk", String.Empty);

			// assert
			Assert.Fail("例外が発生しませんでした。");
		}

		[TestMethod]
		public void ParseMessage_正常() {
			// arrange
			var time = new DateTime(2000, 1, 1);
			var message1 = new Message(1, "hoge", "comment", time, true, false);
			var message2 = new Message(1, "fuga", "comment", time, true, false);
			var message3 = new Message(1, "piyo", "comment", time, true, false);
			var list = new Message[] { message1, message2, message3 };
			var jsonString = DynamicJson.Serialize(new {
				comments = list.Select(item => new {
					comment_num = item.Number,
					message = item.Comment,
					html = "",
					name = item.Name,
					time = JavaScriptTime_Accessor.ToDouble(item.Time, TimeZoneKind.Japan),
					is_ban = item.IsBan,
					auth = item.Auth,
				}),
			});

			// act
			var actual = target.ParseMessage(jsonString);

			// assert
			Assert.AreEqual(message1, actual.ElementAt(0));
			Assert.AreEqual(message2, actual.ElementAt(1));
			Assert.AreEqual(message3, actual.ElementAt(2));

		}

		private sealed class MoqSocketIO : ISocketIOClient {
			private Boolean isConnect;

			#region ISocketIOClient メンバー

			public event Action<object, EventArgs> OnOpen;
			public event Action<object, string> OnMessage;
			public event Action<object, string> OnError;
			public event Action<object, EventArgs> OnClose;

			public string SessionId {
				get { return "1234567890";  }
			}

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