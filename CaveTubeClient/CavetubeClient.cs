namespace CaveTube.CaveTubeClient {
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Diagnostics;
	using System.Linq;
	using System.Net;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Xml;
	using Codeplex.Data;
	using Drumcan.SocketIO;
	using Microsoft.CSharp.RuntimeBinder;

	public sealed class CavetubeClient : IDisposable {
		/// <summary>
		/// 新しいコメントを受信した時に通知されるイベントです。
		/// </summary>
		public event Action<Message> OnMessage;
		/// <summary>
		/// リスナー人数が更新された時に通知されるイベントです。
		/// </summary>
		public event Action<Int32> OnUpdateMember;
		/// <summary>
		/// コメントサーバに接続した時に通知されるイベントです。
		/// </summary>
		public event Action<EventArgs> OnConnect;
		/// <summary>
		/// コメントサーバの接続が切れた時に通知されるイベントです。
		/// </summary>
		public event Action<Reason> OnClose;
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
		/// コメントサーバとの接続状態
		/// </summary>
		public Boolean IsConnect {
			get { return this.client.IsConnect; }
		}
		/// <summary>
		/// 入室している部屋のID
		/// </summary>
		public String JoinedRoomId { get; private set; }

		private ISocketIOClient client;
		private Uri webUri;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="commentUri"></param>
		/// <param name="webUri"></param>
		public CavetubeClient(Uri commentUri, Uri webUri)
			: this(new SocketIOClient(commentUri), webUri) {
		}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="client"></param>
		/// <param name="webUri"></param>
		private CavetubeClient(ISocketIOClient client, Uri webUri) {
			this.webUri = webUri;

			// 配信情報関係
			client.OnMessage += (sender, message) => {
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
				Reason notifyReason;
				switch (reason) {
					case Drumcan.SocketIO.Reason.Timeout:
						notifyReason = Reason.Timeout;
						break;
					default:
						notifyReason = Reason.Unknown;
						break;
				}

				if (this.OnClose != null) {
					this.OnClose(notifyReason);
				}
			};
			this.client = client;
		}

		/// <summary>
		/// CaveTubeコメントサーバに接続します。
		/// </summary>
		/// <exception cref="System.Net.WebException" />
		public void Connect() {
			try {
				this.client.Connect();
			} catch (SocketIOException e) {
				throw new WebException("Cavetubeに接続できません。", e);
			}
		}

		/// <summary>
		/// コメントルームに接続します。
		/// </summary>
		/// <param name="roomId">ルームID</param>
		public void JoinRoom(String roomId) {
			var message = DynamicJson.Serialize(new {
				mode = "join",
				room = roomId,
			});
			client.Send(message);
		}

		/// <summary>
		/// コメントルームから退出します。
		/// </summary>
		public void LeaveRoom() {
			if (this.JoinedRoomId == null) {
				return;
			}

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

		/// <summary>
		/// CaveTubeにログインします。
		/// </summary>
		/// <param name="userId">ユーザー名</param>
		/// <param name="password">パスワード</param>
		/// <param name="devKey">開発者キー</param>
		/// <returns>APIキー</returns>
		/// <exception cref="System.ArgumentException" />
		/// <exception cref="System.Net.WebException" />
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
			using (var client = new WebClient()) {
				var data = new NameValueCollection {
						{"mode", "login"},
						{"devkey", devKey},
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
		}

		/// <summary>
		/// CaveTuneからログアウトします。
		/// </summary>
		/// <param name="userId">ユーザーID</param>
		/// <param name="password">パスワード</param>
		/// <param name="devKey">開発者キー</param>
		/// <returns>ログアウトの成否</returns>
		/// <exception cref="System.ArgumentException" />
		/// <exception cref="System.Net.WebException" />
		public Boolean Logout(String userId, String password, String devKey) {
			if (String.IsNullOrWhiteSpace(userId)) {
				var message = "UserIdが指定されていません。";
				throw new ArgumentException(message);
			}

			if (String.IsNullOrWhiteSpace(password)) {
				var message = "Passwordが指定されていません。";
				throw new ArgumentException(message);
			}

			if (String.IsNullOrWhiteSpace(devKey)) {
				var message = "DevKeyが指定されていません。";
				throw new ArgumentException(message);
			}

			// ログアウト処理に関しても同期処理にします。
			// 一度TPLパターンで実装しましたが、特に必要性を感じなかったので同期に戻しました。
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
		}

		/// <summary>
		/// コメントを投稿します。
		/// </summary>
		/// <param name="name">名前</param>
		/// <param name="message">メッセージ</param>
		/// <param name="apiKey">APIキー</param>
		public void PostComment(String name, String message, String apiKey = "") {
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
			this.client.Send(jsonString);
		}

		/// <summary>
		/// リスナーをBANします。
		/// </summary>
		/// <param name="commentNum">BANするコメント番号</param>
		/// <param name="apiKey">APIキー</param>
		/// <returns>BANの成否</returns>
		/// <exception cref="System.Net.WebException" />
		public Boolean BanListener(Int32 commentNum, String apiKey) {
			return this.SetBanStatus(true, commentNum, apiKey);
		}

		/// <summary>
		/// リスナーのBANを解除します。
		/// </summary>
		/// <param name="commentNum">BAN解除するコメント番号</param>
		/// <param name="apiKey">APIキー</param>
		/// <returns>BAN解除の成否</returns>
		/// <exception cref="System.Net.WebException" />
		public Boolean UnBanListener(Int32 commentNum, String apiKey) {
			return this.SetBanStatus(false, commentNum, apiKey);
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
			if (this.client == null) {
				return;
			}
			this.client.Dispose();
			this.client = null;
		}

		/// <summary>
		/// BANステータスを設定します。
		/// </summary>
		/// <param name="isBan"></param>
		/// <param name="commentNum"></param>
		/// <returns></returns>
		/// <exception cref="System.Net.WebException" />
		private Boolean SetBanStatus(Boolean isBan, Int32 commentNum, String apiKey) {
			if (String.IsNullOrWhiteSpace(apiKey)) {
				throw new ArgumentNullException("APIキーがNullです。");
			}

			using (var client = new WebClient()) {
				var data = new NameValueCollection {
					{"is_ban", isBan ? "true" : "false" },
					{"stream_name", this.JoinedRoomId},
					{"comment_num", commentNum.ToString()},
					{"apikey", apiKey},
				};
				var response = client.UploadValues(String.Format("{0}viewedit/bancomment", this.webUri.AbsoluteUri), "POST", data);
				var jsonString = Encoding.UTF8.GetString(response);

				var json = DynamicJson.Parse(jsonString);
				if (json.IsDefined("ret") && json.ret == false) {
					return false;
				}

				return true;
			}
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
						if (this.OnMessage == null) {
							break;
						}

						var post = new Message(json);
						this.OnMessage(post);
						break;
					case "ban_notify":
						post = new Message(json);

						if (post.IsBan) {
							if (this.OnBan != null) {
								this.OnBan(post);
							}
						} else {
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
			} catch (XmlException) {
				Debug.WriteLine("メッセージのParseに失敗しました。");
			} catch (RuntimeBinderException) {
				Debug.WriteLine("Json内にプロパティが見つかりませんでした。");
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
				this.OnJoin(this.JoinedRoomId);
			}

		}

		~CavetubeClient() {
			this.Dispose();
		}

		public Tuple<Summary, IEnumerable<Message>> GetCavetubeInfomation(String liveUrl) {
			try {
				var streamName = this.ParseStreamUrl(liveUrl);

				using (var client = new WebClient()) {
					client.Encoding = Encoding.UTF8;
					var url = String.Format("{0}viewedit/getcomment?stream_name={1}&comment_num=1", this.webUri.AbsoluteUri, streamName);

					// WebClientでTaskを利用すると正常な順番で結果を受け取れないので、
					// WebRequestを利用する予定ですが、
					// 実装が間に合わないのでまだ同期パターンを使用します。
					var jsonString = client.DownloadString(url);
					var json = DynamicJson.Parse(jsonString);
					if (json.ret == false) {
						throw new CavetubeException("コメントの取得に失敗しました。");
					}

					if (String.IsNullOrEmpty(jsonString)) {
						throw new CavetubeException("コメントの取得に失敗しました。");
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
			} else {
				return String.Empty;
			}
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
			this.Time = JavaScriptTime.ToDateTime(json.time, TimeZoneKind.Japan);
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

	/// <summary>
	/// 切断理由
	/// </summary>
	public enum Reason {
		Timeout,
		Unknown,
	}
}