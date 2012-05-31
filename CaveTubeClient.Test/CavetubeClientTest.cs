namespace CaveTubeClient.Test {
	using System;
	using System.Linq;
	using CaveTube.CaveTubeClient;
	using Codeplex.Data;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using SocketIOClient;

	[TestClass()]
	public class CavetubeClientTest {
		private CavetubeClient_Accessor target = new CavetubeClient_Accessor();

		[TestInitialize()]
		public void MyTestInitialize() {
			var socketIOClient = new MoqSocketIO();
			this.target = new CavetubeClient_Accessor();
		}

		[TestMethod]
		public void OnMessage_モードがpost() {
			// arrange
			Message actMessage = null;

			target.add_OnNewMessage(message => {
				actMessage = message;
			});

			var expSummary = new Summary(DynamicJson_Accessor.Serialize(new {
				room = "room",
				listener = 1,
				viewer = 1
			}));
			var expMessage = this.CreateMessage(1, "", "hoge", "comment", new DateTime(2000, 1, 1), false, false);

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
				time = JavaScriptTime_Accessor.ToDouble(expMessage.Time, TimeZoneKind.Japan),
				auth = false,
				is_ban = false,
			});

			// assert
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
		public void ParseMessage_正常() {
			// arrange
			var time = new DateTime(2000, 1, 1);
			var message1 = this.CreateMessage(1, "", "hoge", "comment", time, true, false);
			var message2 = this.CreateMessage(1, "", "fuga", "comment", time, true, false);
			var message3 = this.CreateMessage(1, "", "piyo", "comment", time, true, false);
			var list = new Message[] { message1, message2, message3 };
			var jsonString = DynamicJson_Accessor.Serialize(new {
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

		private Message CreateMessage(Int32 number, String id, String name, String comment, DateTime time, Boolean auth, Boolean isBan) {
			var json = DynamicJson_Accessor.Serialize(new {
				comment_num = number,
				user_id = id,
				name = name,
				message = comment,
				auth = auth,
				is_ban = isBan,
				time = JavaScriptTime_Accessor.ToDouble(time),
			});
			return new Message(json);
		}

		private sealed class MoqSocketIO : Client {
			public MoqSocketIO() : base("") {

			}

			private Boolean isConnect;

			#region ISocketIOClient メンバー

#pragma warning disable 0067
			public event Action<object, EventArgs> OnOpen;
			public event Action<object, string> OnMessage;
			public event Action<object, string> OnError;
#pragma warning restore 0067

			public string SessionId {
				get { return "1234567890"; }
			}

			public new bool IsConnected {
				get { return isConnect; }
			}

			public new void Connect() {
				this.isConnect = true;
			}

			public new void Close() {
				this.isConnect = false;
			}

			public void Send(string message) {
			}

			#endregion ISocketIOClient メンバー

			#region IDisposable メンバー

			public new void Dispose() {
				this.Close();
			}

			#endregion IDisposable メンバー

			public void TriggerOnMessage(Object obj) {
				var message = DynamicJson_Accessor.Serialize(obj);
				this.OnMessage(null, message);
			}
		}
	
	}
}
