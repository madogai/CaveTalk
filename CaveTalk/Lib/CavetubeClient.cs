namespace CaveTube.CaveTalk.CaveTubeClient {

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
		private Logger logger = LogManager.GetCurrentClassLogger();

		public event Action<Summary, Message> OnMessage;
		public event Action<Int32> OnUpdateMember;
		public event Action<EventArgs> OnConnect;
		public event Action<Reason> OnClose;
		public event Action<Summary, IEnumerable<Message>> OnJoin;
		public event Action<String> OnLeave;
		public event Action<LiveNotification> OnNotifyLive;

		public Boolean IsConnect {
			get { return this.client.IsConnect; }
		}
		public String JoinedRoomId { get; private set; }

		private ISocketIOClient client;
		private Uri webUri;

		public CavetubeClient(Uri commentUri, Uri webUri)
			: this(new SocketIOClient(commentUri), webUri) {
		}

		public CavetubeClient(ISocketIOClient client, Uri webUri) {
			this.webUri = webUri;

			// 配信情報関係
			client.OnMessage += (sender, message) => {
				logger.Debug(message);

				var json = DynamicJson.Parse(message);
				if (json.IsDefined("ret") && json.ret == false) {
					return;
				}

				if (json.IsDefined("mode") == false) {
					return;
				}

				this.HandleMessage(json);
				this.HandleLiveInfomation(json);
				this.HandleJoinOrLeave(json);
			};

			client.OnOpen += (sender, e) => {
				if (this.OnConnect != null) {
					this.OnConnect(e);
				}
			};

			client.OnClose += (sender, reason) => {
				if (this.OnClose != null) {
					this.OnClose(new Reason(reason));
				}
			};
			this.client = client;
		}

		public void Connect() {
			try {
				this.client.Connect();
			} catch (SocketIOException e) {
				throw new WebException("Cavetubeに接続できません。", e);
			}
		}

		public void JoinRoom(String roomId) {
			var message = DynamicJson.Serialize(new {
				mode = "join",
				room = roomId,
			});
			client.Send(message);
		}

		public void LeaveRoom() {
			var message = DynamicJson.Serialize(new {
				mode = "leave",
				room = this.JoinedRoomId,
			});
			client.Send(message);

			if (this.OnLeave != null) {
				this.OnLeave(this.JoinedRoomId);
			}

			this.JoinedRoomId = null;

		}

		public String Login(String userId, String password, String devKey) {
			if (String.IsNullOrWhiteSpace(userId)) {
				throw new ArgumentException("UserIdが指定されていません。");
			}

			if (String.IsNullOrWhiteSpace(password)) {
				throw new ArgumentException("Passwordが指定されていません。");
			}

			if (String.IsNullOrWhiteSpace(devKey)) {
				throw new ArgumentException("DevKeyが指定されていません。");
			}

			// ログイン処理に関しては同期処理にします。
			// 一度TPLパターンで実装しましたが、特に必要性を感じなかったので同期に戻しました。
			try {
				using (var client = new WebClient()) {
					var data = new NameValueCollection {
						{"devkey", devKey},
						{"mode", "login"},
						{"user", userId},
						{"pass", password},
					};
					var response = client.UploadValues(String.Format("{0}api/auth", this.webUri.AbsoluteUri), "POST", data);
					var jsonString = Encoding.UTF8.GetString(response);

					var json = DynamicJson.Parse(jsonString);
					if (json.IsDefined("ret") && json.ret == false) {
						return String.Empty;
					}

					if (json.IsDefined("apikey") == false) {
						return String.Empty;
					}

					return json.apikey;
				}
			} catch (WebException e) {
				logger.Error("CaveTubeサーバにつながりませんでした。", e);
				return String.Empty;
			}
		}

		public Boolean Logout(String userId, String password, String devKey) {
			if (String.IsNullOrWhiteSpace(userId)) {
				var message = "UserIdが指定されていません。";
				logger.Error(message);
				throw new ArgumentException(message);
			}

			if (String.IsNullOrWhiteSpace(password)) {
				var message = "Passwordが指定されていません。";
				logger.Error(message);
				throw new ArgumentException(message);
			}

			if (String.IsNullOrWhiteSpace(devKey)) {
				var message = "DevKeyが指定されていません。";
				logger.Error(message);
				throw new ArgumentException(message);
			}

			// ログアウト処理に関しても同期処理にします。
			// 一度TPLパターンで実装しましたが、特に必要性を感じなかったので同期に戻しました。
			try {
				using (var client = new WebClient()) {
					var data = new NameValueCollection {
						{"devkey", devKey},
						{"mode", "logout"},
						{"user", userId},
						{"pass", password},
					};
					var response = client.UploadValues(String.Format("{0}api/auth", this.webUri.AbsoluteUri), "POST", data);
					var jsonString = Encoding.UTF8.GetString(response);
					var json = DynamicJson.Parse(jsonString);
					if (json.IsDefined("ret") && json.ret == false) {
						return false;
					}
					return true;
				}
			} catch (WebException e) {
				logger.Error("CaveTubeサーバにつながりませんでした。", e);
				return false;
			}
		}

		public void PostComment(String name, String message, String apiKey = "") {
			if (String.IsNullOrWhiteSpace(message)) {
				return;
			}

			try {
				using (var client = new WebClient()) {
					var data = new NameValueCollection {
						{"stream_name", this.JoinedRoomId},
						{"name", name},
						{"message", message},
						{"apikey", apiKey},
					};

					var uri = new Uri(String.Format("{0}viewedit/postcomment", this.webUri.AbsoluteUri));
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

		/// <summary>
		/// コメントと視聴人数などの情報を処理します。
		/// OnMessage以外からは呼ばないでください。
		/// </summary>
		/// <param name="json"></param>
		private void HandleMessage(dynamic json) {
			try {
				String mode = json.mode;
				switch (mode) {
					case "post":
						var roomId = json.IsDefined("room") ? json.room : String.Empty;
						var listener = json.IsDefined("listener") ? (Int32)json.listener : 0;
						var viewer = json.IsDefined("viewer") ? (Int32)json.viewer : 0;
						var summary = new Summary(roomId, listener, viewer);

						var number = json.IsDefined("comment_num") ? (Int32)json.comment_num : 0;
						var name = json.IsDefined("name") ? json.name : String.Empty;
						var message = json.IsDefined("message") ? json.message : String.Empty;
						var auth = json.IsDefined("auth") ? json.auth : false;
						var isBan = json.IsDefined("is_ban") ? json.is_ban : false;

						var time = JavaScriptTime.ToDateTime(json.time, TimeZoneKind.Japan);
						var post = new Message(number, name, message, time, auth, isBan);

						if (this.OnMessage != null) {
							this.OnMessage(summary, post);
						}
						break;
					case "join":
					case "leave":
						if (this.OnUpdateMember == null) {
							break;
						}

						var ipCount = (Int32)json.ipcount;
						if (this.OnUpdateMember != null) {
							this.OnUpdateMember(ipCount);
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
		}

		/// <summary>
		/// 配信情報を処理します。
		/// OnMessage以外からは呼ばないでください。
		/// </summary>
		/// <param name="json"></param>
		private void HandleLiveInfomation(dynamic json) {
			if (json.mode != "start_entry") {
				return;
			}

			if (this.OnNotifyLive != null) {
				var author = json.IsDefined("author") ? json.author : String.Empty;
				var title = json.IsDefined("title") ? json.title : String.Empty;
				var roomId = json.IsDefined("stream_name") ? json.stream_name : String.Empty;
				var liveInfo = new LiveNotification(author, title, roomId);
				this.OnNotifyLive(liveInfo);
			}

		}

		/// <summary>
		/// CaveTalkクライアントの部屋へのJoin/Leave情報を処理します。
		/// </summary>
		/// <param name="json"></param>
		private void HandleJoinOrLeave(dynamic json) {
			if (json.mode != "join") {
				return;
			}

			if (this.client.SessionId != json.id) {
				return;
			}

			this.JoinedRoomId = json.room;

			if (this.OnJoin != null) {
				// 初期コメント取得は非同期でもいいのですが、先にメッセージが来ると面倒なので同期処理にします。
				var tuple = this.GetCavetubeInfomation(this.JoinedRoomId);
				this.OnJoin(tuple.Item1, tuple.Item2);
			}

		}

		~CavetubeClient() {
			this.Dispose();
		}

		private Tuple<Summary, IEnumerable<Message>> GetCavetubeInfomation(String roomId) {
			try {
				using (var client = new WebClient()) {
					client.Encoding = Encoding.UTF8;
					var url = String.Format("{0}viewedit/getcomment2?stream_name={1}&comment_num=1", this.webUri.AbsoluteUri, roomId);

					// WebClientでTaskを利用すると正常な順番で結果を受け取れないので、
					// WebRequestを利用する予定ですが、
					// 実装が間に合わないのでまだ同期パターンを使用します。
					var jsonString = client.DownloadString(url);
					var json = DynamicJson.Parse(jsonString);
					if (json.ret == false) {
						throw new WebException();
					}

					if (String.IsNullOrEmpty(jsonString)) {
						// ここはWebExceptionではない気がする。
						throw new WebException();
					}

					var summary = new Summary(jsonString);
					var messages = this.ParseMessage(jsonString);
					return Tuple.Create(summary, messages);
				}
			} catch (WebException) {
				var summary = new Summary();
				IEnumerable<Message> messages = new List<Message>();
				return Tuple.Create(summary, messages);
			}
		}

		private IEnumerable<Message> ParseMessage(String jsonString) {
			var json = DynamicJson.Parse(jsonString);

			var messages = ((dynamic[])json.comments).Select(comment => {
				var num = (Int32)comment.comment_num;
				var time = JavaScriptTime.ToDateTime(comment.time, TimeZoneKind.Japan);
				var message = new Message(num, comment.name, comment.message, time, comment.auth, comment.is_ban);
				return message;
			});
			return messages;
		}
	}

	public class Summary {
		public String RoomId { get; private set; }

		public Int32 Listener { get; private set; }

		public Int32 PageView { get; private set; }

		public Summary() {
		}

		public Summary(String jsonString) {
			var json = DynamicJson.Parse(jsonString);
			this.RoomId = json.IsDefined("room") ? json.room : String.Empty;
			this.Listener = json.IsDefined("listener") ? (Int32)json.listener : 0;
			this.PageView = json.IsDefined("viewer") ? (Int32)json.viewer : 0;
		}

		public Summary(String roomId, Int32 listener, Int32 viewer) {
			this.RoomId = roomId;
			this.Listener = listener;
			this.PageView = viewer;
		}

		public override bool Equals(object obj) {
			var other = obj as Summary;
			if (other == null) {
				return false;
			}

			var isRoomIdSame = this.RoomId == other.RoomId;
			var isListenerSame = this.Listener == other.Listener;
			var isPageViewSame = this.PageView == other.PageView;
			return isListenerSame && isPageViewSame;
		}

		public override int GetHashCode() {
			return this.RoomId.GetHashCode() ^ this.Listener.GetHashCode() ^ this.PageView.GetHashCode();
		}
	}

	public class Message {

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

	public class LiveNotification {
		public String Author { get; private set; }

		public String Title { get; private set; }

		public String RoomId { get; private set; }

		public LiveNotification(String author, String title, String roomId) {
			this.Author = author;
			this.Title = title;
			this.RoomId = roomId;
		}

		public override Boolean Equals(object obj) {
			var other = obj as LiveNotification;
			if (other == null) {
				return false;
			}

			var isAuthorSame = this.Author == other.Author;
			var isTitleSame = this.Title == other.Title;
			var isRoomIdSame = this.RoomId == other.RoomId;

			return isAuthorSame && isTitleSame && isRoomIdSame;
		}

		public override Int32 GetHashCode() {
			return this.Author.GetHashCode() ^ this.Title.GetHashCode() ^ this.RoomId.GetHashCode();
		}
	}

	public class Reason : SocketIO.Reason {
		public Reason(SocketIO.Reason parent)
			: base(parent.IsTimeout) {
		}
	}
}