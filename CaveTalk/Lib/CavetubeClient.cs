namespace CaveTube.CaveTalk {

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Text;
	using CaveTube.CaveTalk.Utils;
	using Codeplex.Data;
	using SocketIO;
	using Microsoft.CSharp.RuntimeBinder;
	using NLog;
	using System.Xml;

	public sealed class CavetubeClient : IDisposable {
		private const String socketBase = "http://ws.cavelis.net:3000/socket.io/1/";
		private const String restBase = "http://gae.cavelis.net/viewedit/getcomment?stream_name={0}&comment_num=1";

		public event Action<Object, Summary, Message> OnMessage;
		public event Action<Object, Int32> OnUpdateMember;
		public event Action<Summary, IEnumerable<Message>> OnConnect;
		public event Action<Object, EventArgs> OnClose;

		public Boolean IsConnect {
			get { return this.client.IsConnect; }
		}

		private Logger logger = LogManager.GetLogger("CavetubeClient");
		private String roomId;
		private ISocketIOClient client;

		public CavetubeClient()
			: this(new Uri(socketBase)) {
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
						var summary = this.ParseSummary(jsonString);
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
					this.OnConnect(tuple.Item1, tuple.Item2);
				}

				var msg = DynamicJson.Serialize(new {
					mode = "join",
					room = this.roomId,
				});
				client.Send(msg);
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
							var summary = new Summary {
								Listener = (Int32)json.listener,
								PageView = (Int32)json.viewer,
							};

							var post = new Message {
								Number = (Int32)json.comment_num,
								Name = json.name,
								Comment = json.message,
								Time = JavaScriptTime.ToDateTime(json.time, TimeZoneKind.Japan),
							};
							if (this.OnMessage != null) {
								this.OnMessage(sender, summary, post);
							}
							break;
						case "join":
						case "leave":
							if (this.OnUpdateMember != null) {
								var listener = (Int32)json.ipcount;
								if (this.OnUpdateMember != null) {
									this.OnUpdateMember(sender, listener);
								}
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
			client.OnClose += (obj, e) => {
				if (this.OnClose != null) {
					this.OnClose(obj, e);
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
					var url = String.Format(restBase, roomId);
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

		private Summary ParseSummary(String jsonString) {
			var json = DynamicJson.Parse(jsonString);
			return new Summary {
				Listener = (Int32)json.listener,
				PageView = (Int32)json.viewer,
			};
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
				return new Message {
					Number = num,
					Name = comment.name,
					Comment = comment.message,
					Time = JavaScriptTime.ToDateTime(comment.time, TimeZoneKind.Japan),
				};
			});
			return messages;
		}
	}

	public sealed class Summary {

		public Int32 Listener { get; set; }

		public Int32 PageView { get; set; }

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

		public Int32 Number { get; set; }

		public String Name { get; set; }

		public String Comment { get; set; }

		public DateTime Time { get; set; }

		public override bool Equals(object obj) {
			var other = obj as Message;
			if (other == null) {
				return false;
			}

			var isNumberSame = this.Number == other.Number;
			var isNameSame = this.Name == other.Name;
			var isCommentSame = this.Comment == other.Comment;
			var isTimeSame = this.Time == other.Time;

			return isNumberSame && isNameSame && isCommentSame && isTimeSame;
		}

		public override int GetHashCode() {
			return this.Number.GetHashCode() ^ this.Name.GetHashCode() ^ this.Comment.GetHashCode() ^ this.Time.GetHashCode();
		}
	}
}