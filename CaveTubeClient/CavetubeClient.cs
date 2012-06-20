namespace CaveTube.CaveTubeClient {
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Diagnostics;
	using System.Linq;
	using System.Net;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Xml;
	using Codeplex.Data;
	using Microsoft.CSharp.RuntimeBinder;
	using SocketIOClient;
	using SocketIOClient.Messages;

	public sealed class CavetubeClient : IDisposable {
		private static String webUrl = ConfigurationManager.AppSettings["web_server"] ?? "http://gae.cavelis.net";
		private static String socketIOUrl = ConfigurationManager.AppSettings["comment_server"] ?? "http://ws.vmhost:3000";

		/// <summary>
		/// メッセージ一覧を取得した時に通知されるイベントです。
		/// </summary>
		public event Action<IEnumerable<Message>> OnMessageList;
		/// <summary>
		/// 新しいコメントを受信した時に通知されるイベントです。
		/// </summary>
		public event Action<Message> OnNewMessage;
		/// <summary>
		/// リスナー人数が更新された時に通知されるイベントです。
		/// </summary>
		public event Action<Int32> OnUpdateMember;
		/// <summary>
		/// コメントサーバに接続した時に通知されるイベントです。
		/// </summary>
		public event Action OnConnect;
		/// <summary>
		/// コメントサーバの接続が切れた時に通知されるイベントです。
		/// </summary>
		public event Action OnDisconnect;
		/// <summary>
		/// コメントルームに入室した時に通知されるイベントです。
		/// </summary>
		public event Action<String> OnJoin;
		/// <summary>
		/// コメントルームから退出した時に通知されるイベントです。
		/// </summary>
		public event Action<String> OnLeave;
		/// <summary>
		/// リスナーがBANされた時に通知されるイベントです。
		/// </summary>
		public event Action<Message> OnBan;
		/// <summary>
		/// リスナーのBANが解除された時に通知されるイベントです。
		/// </summary>
		public event Action<Message> OnUnBan;
		/// <summary>
		/// 新しい配信が始まった時に通知されるイベントです。
		/// </summary>
		public event Action<LiveNotification> OnNotifyLive;
		/// <summary>
		/// 何かしらのエラーが発生したときに通知されるイベントです。
		/// </summary>
		public event Action<CavetubeException> OnError;

		private event Action<dynamic> OnMessage;

		/// <summary>
		/// コメントサーバとの接続状態
		/// </summary>
		public Boolean IsConnect {
			get { return this.client.IsConnected; }
		}
		/// <summary>
		/// 入室している部屋のID
		/// </summary>
		public String JoinedRoomId { get; private set; }
		/// <summary>
		/// 入室している部屋のオーナー名
		/// </summary>
		public String JoinedRoomAuthor { get; private set; }

		private Uri webUri;
		private Uri socketIOUri;
		private Client client;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="client"></param>
		public CavetubeClient() {
			this.webUri = new Uri(webUrl);
			this.socketIOUri = new Uri(socketIOUrl);
			this.client = new Client(socketIOUrl);

			this.client.RetryConnectionAttempts = 15;

			// 配信情報関係
			this.client.On("message", message => {
				var json = DynamicJson.Parse(message.MessageText);
				if (json.IsDefined("ret") && json.ret == false) {
					return;
				}

				if (json.IsDefined("mode") == false) {
					return;
				}

				if (this.OnMessage != null) {
					this.OnMessage(json);
				}
			});

			this.client.Opened += (sender, e) => {
				if (this.OnConnect != null) {
					this.OnConnect();
				}

				if (String.IsNullOrWhiteSpace(this.JoinedRoomId) == false) {
					this.JoinRoom(this.JoinedRoomId);
				}
			};

			this.client.Error += (sender, e) => {
				if (this.OnError != null) {
					this.OnError(new CavetubeException(e.Message, e.Exception));
				}
			};

			this.client.SocketConnectionClosed += (sender, args) => {
				if (this.OnDisconnect != null) {
					this.OnDisconnect();
				}
			};

			this.OnMessage += this.HandleMessage;
			this.OnMessage += this.HandleLiveInfomation;
			this.OnMessage += this.HandleJoin;
		}

		~CavetubeClient() {
			this.Dispose();
		}

		/// <summary>
		/// CaveTubeコメントサーバに接続します。
		/// </summary>
		/// <exception cref="System.Net.WebException" />
		public void Connect() {
			try {
				this.client.Connect();
			}
			catch (Exception ex) {
				throw new CavetubeException("CaveTubeとの接続に失敗しました。", ex);
			}
		}

		/// <summary>
		/// 部屋の情報を取得します。
		/// </summary>
		/// <param name="liveUrl">配信URL</param>
		/// <returns></returns>
		public Summary GetSummary(String liveUrl) {
			try {
				var streamName = this.ParseStreamUrl(liveUrl);

				using (var client = new WebClient()) {
					client.Encoding = Encoding.UTF8;
					var url = String.Format("{0}://{1}:{2}/viewedit/get?stream_name={3}", this.webUri.Scheme, this.webUri.Host, this.webUri.Port, streamName);

					// WebClientでTaskを利用すると正常な順番で結果を受け取れないので、
					// WebRequestを利用する予定ですが、
					// 実装が間に合わないのでまだ同期パターンを使用します。
					var jsonString = client.DownloadString(url);
					if (String.IsNullOrEmpty(jsonString)) {
						throw new CavetubeException("サマリーの取得に失敗しました。");
					}

					var json = DynamicJson.Parse(jsonString);
					if (json.ret == false) {
						throw new CavetubeException("サマリーの取得に失敗しました。");
					}

					var summary = new Summary(jsonString);
					return summary;
				}
			}
			catch (WebException) {
				return new Summary();
			}
		}

		/// <summary>
		/// コメントの取得リクエストを送信します。
		/// </summary>
		/// <param name="url">配信URL</param>
		public IEnumerable<Message> GetComment(String liveUrl) {
			try {
				var streamName = this.ParseStreamUrl(liveUrl);

				using (var client = new WebClient()) {
					client.Encoding = Encoding.UTF8;
					client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
					var url = String.Format("{0}://{1}:{2}/comment/{3}", this.socketIOUri.Scheme, this.socketIOUri.Host, this.socketIOUri.Port, streamName);

					var jsonString = client.DownloadString(url);
					if (String.IsNullOrEmpty(jsonString)) {
						throw new CavetubeException("コメントの取得に失敗しました。");
					}

					var json = DynamicJson.Parse(jsonString);
					if (json.ret == false) {
						throw new CavetubeException("コメントの取得に失敗しました。");
					}

					var comments = this.ParseMessage(json);
					return comments;
				}
			}
			catch (WebException) {
				return new List<Message>();
			}
		}

		/// <summary>
		/// コメントルームに接続します。
		/// </summary>
		/// <param name="roomId">視聴ページのURL、またはルームID</param>
		/// <exception cref="System.FormatException">引数のフォーマットが正しくありません。</exception>
		public void JoinRoom(String liveUrl) {
			var roomId = this.ParseStreamUrl(liveUrl);
			if (String.IsNullOrWhiteSpace(roomId)) {
				throw new FormatException("URLのフォーマットが正常ではありません。");
			}

			var message = DynamicJson.Serialize(new {
				mode = "join",
				room = roomId,
			});
			client.Send(new TextMessage(message));
		}

		/// <summary>
		/// コメントルームから退出します。
		/// </summary>
		public void LeaveRoom() {
			if (String.IsNullOrWhiteSpace(this.JoinedRoomId)) {
				return;
			}

			var message = DynamicJson.Serialize(new {
				mode = "leave",
				room = this.JoinedRoomId,
			});
			client.Send(new TextMessage(message));

			if (this.OnLeave != null) {
				this.OnLeave(this.JoinedRoomId);
			}

			this.JoinedRoomId = null;
			this.JoinedRoomAuthor = null;
		}

		/// <summary>
		/// コメントを投稿します。
		/// </summary>
		/// <param name="name">名前</param>
		/// <param name="message">メッセージ</param>
		/// <param name="apiKey">APIキー</param>
		public void PostComment(String name, String message, String apiKey = "") {
			if (String.IsNullOrWhiteSpace(this.JoinedRoomId)) {
				throw new CavetubeException("部屋に所属していません。");
			}

			if (String.IsNullOrWhiteSpace(message)) {
				return;
			}

			var jsonString = DynamicJson.Serialize(new {
				mode = "post",
				stream_name = this.JoinedRoomId,
				name = name,
				message = message,
				apikey = apiKey,
				_session = "cavetalk",
			});
			client.Send(new TextMessage(jsonString));
		}

		/// <summary>
		/// リスナーをBANします。
		/// </summary>
		/// <param name="commentNum">BANするコメント番号</param>
		/// <param name="apiKey">APIキー</param>
		/// <exception cref="System.ArgumentException"></exception>
		public void BanListener(Int32 commentNum, String apiKey) {
			if (String.IsNullOrWhiteSpace(apiKey)) {
				throw new ArgumentException("APIキーは必須です。");
			}

			if (String.IsNullOrWhiteSpace(this.JoinedRoomId)) {
				throw new CavetubeException("部屋に所属していません。");
			}

			var message = DynamicJson.Serialize(new {
				mode = "ban",
				comment_num = commentNum,
				api_key = apiKey,
			});
			client.Send(new TextMessage(message));
		}

		/// <summary>
		/// リスナーのBANを解除します。
		/// </summary>
		/// <param name="commentNum">BAN解除するコメント番号</param>
		/// <param name="apiKey">APIキー</param>
		/// <exception cref="System.ArgumentException"></exception>
		public void UnBanListener(Int32 commentNum, String apiKey) {
			if (String.IsNullOrWhiteSpace(apiKey)) {
				throw new ArgumentException("APIキーは必須です。");
			}

			if (String.IsNullOrWhiteSpace(this.JoinedRoomId)) {
				throw new CavetubeException("部屋に所属していません。");
			}

			var message = DynamicJson.Serialize(new {
				mode = "unban",
				comment_num = commentNum,
				api_key = apiKey,
			});
			client.Send(new TextMessage(message));
		}

		/// <summary>
		/// コメントサーバとの接続を閉じます。
		/// </summary>
		public void Close() {
			if (this.client == null) {
				return;
			}
			this.client.Close();
		}

		/// <summary>
		/// オブジェクトを破棄します。
		/// </summary>
		public void Dispose() {
			this.OnMessage -= this.HandleMessage;
			this.OnMessage -= this.HandleLiveInfomation;
			this.OnMessage -= this.HandleJoin;

			if (this.client == null) {
				return;
			}
			this.client.Dispose();
			this.client = null;
		}

		/// <summary>
		/// コメントと視聴人数などの情報を処理します。
		/// </summary>
		/// <param name="json"></param>
		private void HandleMessage(dynamic json) {
			try {
				String mode = json.mode;
				switch (mode) {
					case "get":
						if (this.OnMessageList == null) {
							break;
						}

						var messages = this.ParseMessage(json);
						this.OnMessageList(messages);
						break;
					case "post":
						if (this.OnNewMessage == null) {
							break;
						}

						var post = new Message(json);
						this.OnNewMessage(post);
						break;
					case "ban_notify":
						post = new Message(json);

						if (post.IsBan) {
							if (this.OnBan != null) {
								this.OnBan(post);
							}
						}
						else {
							if (this.OnUnBan != null) {
								this.OnUnBan(post);
							}
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
			}
			catch (XmlException) {
				Debug.WriteLine("メッセージのParseに失敗しました。");
			}
			catch (RuntimeBinderException) {
				Debug.WriteLine("Json内にプロパティが見つかりませんでした。");
			}
		}

		/// <summary>
		/// 配信情報を処理します。
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
		private void HandleJoin(dynamic json) {
			if (json.mode != "ready") {
				return;
			}

			var summary = this.GetSummary((String)json.room);
			if (summary.RoomId == null) {
				return;
			}

			this.JoinedRoomAuthor = summary.Author;
			this.JoinedRoomId = summary.RoomId;

			if (this.OnJoin != null) {
				this.OnJoin(this.JoinedRoomId);
			}
		}

		private IEnumerable<Message> ParseMessage(dynamic json) {
			var messages = ((dynamic[])json.comments).Select(comment => new Message(comment));
			return messages;
		}

		private String ParseStreamUrl(String url) {
			var baseUrl = String.Format("{0}://{1}", this.webUri.Scheme, this.webUri.Host);
			if (this.webUri.Port != 80) {
				baseUrl += String.Format(":{0}", this.webUri.Port);
			}

			var pattern = String.Format(@"^(?:{0}/[a-z]+/)?([0-9A-Z]{{32}})", baseUrl);
			var match = Regex.Match(url, pattern);
			if (match.Success) {
				return match.Groups[1].Value;
			}

			pattern = String.Format(@"^(?:{0}/live/(.*))", baseUrl);
			match = Regex.Match(url, pattern);
			if (match.Success) {
				using (var client = new WebClient()) {
					var userName = match.Groups[1].Value;
					var jsonString = client.DownloadString(String.Format("{0}/live_url?user={1}", baseUrl, userName));
					var json = DynamicJson.Parse(jsonString);
					var streamName = json.IsDefined("stream_name") ? json.stream_name : String.Empty;
					return streamName;
				}
			}

			return String.Empty;
		}
	}

	/// <summary>
	/// 配信概要
	/// </summary>
	public class Summary {
		public String RoomId { get; set; }
		public String Title { get; set; }
		public String Author { get; set; }
		public Int32 Listener { get; set; }
		public Int32 PageView { get; set; }
		public DateTime StartTime { get; set; }

		public Summary() {
		}

		public Summary(String jsonString) {
			var json = DynamicJson.Parse(jsonString);
			this.RoomId = json.IsDefined("stream_name") ? json.stream_name : String.Empty;
			this.Title = json.IsDefined("title") ? json.title : String.Empty;
			this.Author = json.IsDefined("author") ? json.author : String.Empty;
			this.Listener = json.IsDefined("listener") ? (Int32)json.listener : 0;
			this.PageView = json.IsDefined("viewer") ? (Int32)json.viewer : 0;
			this.StartTime = json.IsDefined("start_time") && json.start_time != null ? JavaScriptTime.ToDateTime(json.start_time, TimeZoneKind.Japan) : null;
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

	/// <summary>
	/// コメント情報
	/// </summary>
	public class Message {

		public Int32 Number { get; set; }

		public String Id { get; set; }

		public String Name { get; set; }

		public String Comment { get; set; }

		public DateTime Time { get; set; }

		public Boolean Auth { get; set; }

		public Boolean IsBan { get; set; }

		public Message(dynamic json) {
			this.Number = json.IsDefined("comment_num") ? (Int32)json.comment_num : 0;
			this.Id = json.IsDefined("user_id") ? (String)json.user_id : String.Empty;
			this.Name = json.IsDefined("name") ? json.name : String.Empty;
			this.Comment = json.IsDefined("message") ? json.message : String.Empty;
			this.Auth = json.IsDefined("auth") ? json.auth : false;
			this.IsBan = json.IsDefined("is_ban") ? json.is_ban : false;
			this.Time = json.IsDefined("time") ? JavaScriptTime.ToDateTime(json.time, TimeZoneKind.Japan) : null;
		}

		public override bool Equals(object obj) {
			var other = obj as Message;
			if (other == null) {
				return false;
			}

			var isNumberSame = this.Number == other.Number;
			var isCommentSame = this.Comment == other.Comment;

			return isNumberSame && isCommentSame;
		}

		public override int GetHashCode() {
			return this.Number.GetHashCode() ^ this.Comment.GetHashCode();
		}
	}

	/// <summary>
	/// 配信通知情報
	/// </summary>
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
			var isRoomIdSame = this.RoomId == other.RoomId;

			return isAuthorSame && isRoomIdSame;
		}

		public override Int32 GetHashCode() {
			return this.Author.GetHashCode() ^ this.RoomId.GetHashCode();
		}
	}
}