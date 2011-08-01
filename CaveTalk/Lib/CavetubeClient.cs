namespace CaveTube.CaveTalk {

	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Linq;
	using System.Net;
	using System.Text;
	using System.Xml;
	using CaveTube.CaveTalk.Utils;
	using Codeplex.Data;
	using Microsoft.CSharp.RuntimeBinder;
	using NLog;
	using SocketIO;

	public sealed class CavetubeClient : IDisposable {
		public event Action<Object, Summary, Message> OnMessage;
		public event Action<Object, Int32> OnUpdateMember;
		public event Action<Object, Summary, IEnumerable<Message>> OnConnect;
		public event Action<Object, EventArgs> OnClose;

		public Boolean IsConnect {
			get { return this.client.IsConnect; }
		}

		private Logger logger = LogManager.GetLogger("CavetubeClient");
		private String roomId;
		private ISocketIOClient client;

		public CavetubeClient()
			: this(new Uri("http://ws.cavelis.net:3000/socket.io/1/")) {
		}

		public CavetubeClient(Uri uri)
			: this(new SocketIOClient(uri)) {
		}

		public CavetubeClient(ISocketIOClient client) {
			client.OnOpen += (sender, e) => {
				// 初期コメント取得は非同期でもいいのですが、先にメッセージが来ると面倒なので同期処理にします。
				Func<String, Tuple<Summary, IEnumerable<Message>>> getInfomation = roomId => {
					var jsonString = this.GetCavetubeInfomation(roomId);
					if (String.IsNullOrEmpty(jsonString) == false) {
						var summary = new Summary(jsonString);
						var messages = this.ParseMessage(jsonString);
						return Tuple.Create(summary, messages);
					} else {
						var summary = new Summary();
						IEnumerable<Message> messages = new List<Message>();
						return Tuple.Create(summary, messages);
					}
				};
				var tuple = getInfomation(this.roomId);
				if (this.OnConnect != null) {
					this.OnConnect(this, tuple.Item1, tuple.Item2);
				}

				var message = DynamicJson.Serialize(new {
					mode = "join",
					room = this.roomId,
				});
				client.Send(message);
			};

			client.OnMessage += (sender, message) => {
				logger.Debug(message);
				try {
					var json = DynamicJson.Parse(message);
					if (json.IsDefined("ret") && json.ret == false) {
						return;
					}

					if (json.IsDefined("mode") == false) {
						return;
					}

					String mode = json.mode;
					switch (mode) {
						case "post":
							var listener = (Int32)json.listener;
							var pageView = (Int32)json.viewer;
							var summary = new Summary(listener, pageView);

							var number = (Int32)json.comment_num;
							var time = JavaScriptTime.ToDateTime(json.time, TimeZoneKind.Japan);
							var post = new Message(number, json.name, json.message, time, json.auth, json.is_ban);

							if (this.OnMessage != null) {
								this.OnMessage(this, summary, post);
							}
							break;
						case "join":
						case "leave":
							if (this.OnUpdateMember == null) {
								break;
							}

							var ipCount = (Int32)json.ipcount;
							if (this.OnUpdateMember != null) {
								this.OnUpdateMember(this, ipCount);
							}
							break;
						default:
							break;
					}
				} catch (XmlException) {
					logger.Warn("メッセージのParseに失敗しました。");
				} catch (RuntimeBinderException) {
					logger.Warn("Json内にプロパティが見つかりませんでした。");
				}
			};
			client.OnClose += (sender, e) => {
				if (this.OnClose != null) {
					this.OnClose(this, e);
				}
			};
			this.client = client;
		}

		public void Connect(String roomId) {
			this.roomId = roomId;
			try {
				this.client.Connect();
			} catch (SocketIOException e) {
				throw new WebException("Cavetubeに接続できません。", e);
			}
		}

		public void PostComment(String name, String message) {
			if (String.IsNullOrWhiteSpace(message)) {
				return;
			}

			try {
				using (var client = new WebClient()) {
					var data = new NameValueCollection {
						{"stream_name", this.roomId},
						{"name", name},
						{"message", message},
					};

					var uri = new Uri("http://gae.cavelis.net/viewedit/postcomment");
					client.UploadValuesAsync(uri, data);
				}
			} catch (WebException e) {
				logger.Error("コメントの投稿に失敗しました。", e);
			}
		}

		public void Close() {
			if (this.client == null) {
				return;
			}
			this.client.Close();
		}

		public void Dispose() {
			if (this.client == null) {
				return;
			}
			this.client.Dispose();
			this.client = null;
		}

		~CavetubeClient() {
			this.Dispose();
		}

		private String GetCavetubeInfomation(String roomId) {
			try {
				using (var client = new WebClient()) {
					client.Encoding = Encoding.UTF8;
					var url = String.Format("http://gae.cavelis.net/viewedit/getcomment?stream_name={0}&comment_num=1", roomId);
					var jsonString = client.DownloadString(url);
					var json = DynamicJson.Parse(jsonString);
					if (json.ret == false) {
						throw new WebException();
					}
					return jsonString;
				}
			} catch (WebException) {
				return String.Empty;
			}
		}

		private IEnumerable<Message> ParseMessage(String jsonString) {
			var json = DynamicJson.Parse(jsonString);
			var commentCount = json.IsDefined("comment_num") ? (Int32)json.comment_num : 0;
			var messages = Enumerable.Range(1, commentCount).Where(num => {
				var attr = String.Format("num_{0}", num);
				return json.IsDefined(attr);
			}).Select(num => {
				var attr = String.Format("num_{0}", num);
				var comment = json[attr];

				var time = JavaScriptTime.ToDateTime(comment.time, TimeZoneKind.Japan);
				var message = new Message(num, comment.name, comment.message, time, comment.auth, comment.is_ban);
				return message;
			});
			return messages;
		}
	}

	public sealed class Summary {

		public Int32 Listener { get; private set; }

		public Int32 PageView { get; private set; }

		public Summary() {
		}

		public Summary(Int32 listener, Int32 pageView) {
			this.Listener = listener;
			this.PageView = pageView;
		}

		public Summary(String jsonString) {
			var json = DynamicJson.Parse(jsonString);
			this.Listener = (Int32)json.listener;
			this.PageView = (Int32)json.viewer;
		}

		public override bool Equals(object obj) {
			var other = obj as Summary;
			if (other == null) {
				return false;
			}

			var isListenerSame = this.Listener == other.Listener;
			var isPageViewSame = this.PageView == other.PageView;
			return isListenerSame && isPageViewSame;
		}

		public override int GetHashCode() {
			return this.Listener.GetHashCode() ^ this.PageView.GetHashCode();
		}
	}

	public sealed class Message {

		public Int32 Number { get; private set; }

		public String Name { get; private set; }

		public String Comment { get; private set; }

		public DateTime Time { get; private set; }

		public Boolean Auth { get; private set; }

		public Boolean IsBan { get; private set; }

		public Message(Int32 number, String name, String comment, DateTime time, Boolean auth, Boolean isBan) {
			this.Number = number;
			this.Name = name;
			this.Comment = comment;
			this.Time = time;
			this.Auth = auth;
			this.IsBan = isBan;
		}

		public override bool Equals(object obj) {
			var other = obj as Message;
			if (other == null) {
				return false;
			}

			var isNumberSame = this.Number == other.Number;
			var isNameSame = this.Name == other.Name;
			var isCommentSame = this.Comment == other.Comment;
			var isTimeSame = this.Time == other.Time;
			var isAuthSame = this.Auth == other.Auth;
			var isIsBanSame = this.IsBan == other.IsBan;

			return isNumberSame && isNameSame && isCommentSame && isTimeSame && isAuthSame && isIsBanSame;
		}

		public override int GetHashCode() {
			return this.Number.GetHashCode() ^ this.Name.GetHashCode() ^ this.Comment.GetHashCode() ^ this.Time.GetHashCode() ^ this.Auth.GetHashCode() ^ this.IsBan.GetHashCode();
		}
	}
}