namespace CaveTube.CaveTubeClient {
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Diagnostics;
	using System.Linq;
	using System.Net;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Xml;
	using Codeplex.Data;
	using Microsoft.CSharp.RuntimeBinder;
	using SocketIOClient;
	using SocketIOClient.Messages;

	public sealed class CavetubeClient : IDisposable {
		private static String webUrl = ConfigurationManager.AppSettings["web_server"] ?? "http://gae.cavelis.net";
		private static String socketIOUrl = ConfigurationManager.AppSettings["comment_server"] ?? "http://ws.cavelis.net:3000";
		private static String devkey = ConfigurationManager.AppSettings["dev_key"] ?? String.Empty;

		/// <summary>
		/// メッセージ一覧を取得した時に通知されるイベントです。
		/// </summary>
		public event Action<IEnumerable<Message>> OnMessageList;
		/// <summary>
		/// 新しいコメントを受信した時に通知されるイベントです。
		/// </summary>
		public event Action<Message> OnNewMessage;
		/// <summary>
		/// コメント投稿の結果を受信したときに通知されるイベントです。
		/// </summary>
		public event Action<Boolean> OnPostResult;
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
		/// BAN操作に失敗したときに通知されるイベントです。
		/// </summary>
		public event Action<BanFail> OnBanFail;
		/// <summary>
		/// リスナーへのID強制表示が行われたときに通知されるイベントです。
		/// </summary>
		public event Action<IdNotification> OnShowId;
		/// <summary>
		/// リスナーへのID強制表示が解除されたときに通知されるイベントです。
		/// </summary>
		public event Action<IdNotification> OnHideId;
		/// <summary>
		/// コメントが非表示指定された時に通知されるイベントです。
		/// </summary>
		public event Action<Message> OnHideComment;
		/// <summary>
		/// コメントが再表示指定された時に通知されるイベントです。
		/// </summary>
		public event Action<Message> OnShowComment;
		/// <summary>
		/// 新しい配信が始まった時に通知されるイベントです。
		/// </summary>
		public event Action<LiveNotification> OnNotifyLiveStart;
		/// <summary>
		/// 配信が終了したときに通知されるイベントです。
		/// </summary>
		public event Action<LiveNotification> OnNotifyLiveClose;
		/// <summary>
		/// 管理者メッセージ通知です。
		/// </summary>
		public event Action<AdminShout> OnAdminShout;
		/// <summary>
		/// 何かしらのエラーが発生したときに通知されるイベントです。
		/// </summary>
		public event Action<CavetubeException> OnError;

		private event Action<dynamic> OnMessage;

		/// <summary>
		/// ソケットID
		/// </summary>
		public String SocketId {
			get {
				if (this.IsConnect == false) {
					return String.Empty;
				}
				return this.client.HandShake.SID;
			}
		}
		/// <summary>
		/// コメントサーバとの接続状態
		/// </summary>
		public Boolean IsConnect { get { return this.client.IsConnected; } }
		/// <summary>
		/// 入室している部屋のサマリー
		/// </summary>
		public Summary JoinedRoom { get; private set; }

		private Uri webUri;
		private Uri socketIOUri;
		private Client client;
		private ManualResetEvent connectionOpenEvent;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="client"></param>
		public CavetubeClient() {
			this.webUri = new Uri(webUrl);
			this.socketIOUri = new Uri(socketIOUrl);
			this.connectionOpenEvent = new ManualResetEvent(false);
			this.client = new Client(socketIOUrl);
			this.client.RetryConnectionAttempts = 15;
			this.client.Opened += (sender, e) => {
				this.connectionOpenEvent.Set();
			};

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

				if (this.JoinedRoom != null && String.IsNullOrWhiteSpace(this.JoinedRoom.RoomId) == false) {
					this.JoinRoom(this.JoinedRoom.RoomId);
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

			this.OnMessage += this.HandleCommentInformation;
			this.OnMessage += this.HandleRoomInformation;
			this.OnMessage += this.HandleLiveInfomation;
			this.OnMessage += this.HandleOtherInformation;
		}

		~CavetubeClient() {
			this.Dispose();
		}

		/// <summary>
		/// CaveTubeコメントサーバに接続します。
		/// </summary>
		/// <exception cref="System.Net.WebException" />
		public void Connect() {
			this.connectionOpenEvent.Reset();
			this.client.Connect();
			var isConnect = this.connectionOpenEvent.WaitOne(4000);
			if (isConnect == false) {
				throw new CavetubeException("CaveTubeとの接続に失敗しました。");
			}
		}

		/// <summary>
		/// 部屋の情報を取得します。
		/// </summary>
		/// <param name="liveUrl">配信URL</param>
		/// <returns></returns>
		public async Task<Summary> GetSummaryAsync(String liveUrl) {
			try {
				var streamName = await this.ParseStreamUrlAsync(liveUrl);
				if (String.IsNullOrWhiteSpace(streamName)) {
					throw new CavetubeException("サマリーの取得に失敗しました。");
				}

				using (var client = new WebClient()) {
					client.Encoding = Encoding.UTF8;
					client.QueryString.Add("stream_name", streamName);
					client.QueryString.Add("devkey", devkey);
					var url = String.Format("{0}://{1}:{2}/api/summary", this.webUri.Scheme, this.webUri.Host, this.webUri.Port);
					var jsonString = await client.DownloadStringTaskAsync(url);
					if (String.IsNullOrEmpty(jsonString)) {
						throw new CavetubeException("サマリーの取得に失敗しました。");
					}

					var json = DynamicJson.Parse(jsonString);
					return new Summary(jsonString);
				}
			} catch (WebException e) {
				throw new CavetubeException("サマリーの取得に失敗しました", e);
			}
		}

		/// <summary>
		/// コメントの取得リクエストを送信します。
		/// </summary>
		/// <param name="url">配信URL</param>
		public async Task<IEnumerable<Message>> GetCommentAsync(String liveUrl) {
			try {
				var streamName = await this.ParseStreamUrlAsync(liveUrl);

				using (var client = new WebClient()) {
					client.Encoding = Encoding.UTF8;
					client.QueryString.Add("devkey", devkey);
					var url = String.Format("{0}://{1}:{2}/comment/{3}", this.socketIOUri.Scheme, this.socketIOUri.Host, this.socketIOUri.Port, streamName);

					var jsonString = await client.DownloadStringTaskAsync(url);
					if (String.IsNullOrEmpty(jsonString)) {
						throw new CavetubeException("コメントの取得に失敗しました。");
					}

					var json = DynamicJson.Parse(jsonString);
					var comments = this.ParseMessage(json);
					return comments;
				}
			} catch (WebException) {
				return new List<Message>();
			}
		}

		/// <summary>
		/// コメントルームに接続します。
		/// </summary>
		/// <param name="roomId">視聴ページのURL、またはルームID</param>
		/// <exception cref="System.FormatException">引数のフォーマットが正しくありません。</exception>
		public async void JoinRoom(String liveUrl) {
			var roomId = await this.ParseStreamUrlAsync(liveUrl);
			if (String.IsNullOrWhiteSpace(roomId)) {
				throw new FormatException("URLのフォーマットが正常ではありません。");
			}

			var message = DynamicJson.Serialize(new {
				devkey = devkey,
				mode = "join",
				room = roomId,
			});
			client.Send(new TextMessage(message));
		}

		/// <summary>
		/// コメントルームから退出します。
		/// </summary>
		public void LeaveRoom() {
			var roomId = this.JoinedRoom.RoomId;
			if (String.IsNullOrWhiteSpace(roomId)) {
				return;
			}

			var message = DynamicJson.Serialize(new {
				devkey = devkey,
				mode = "leave",
				room = roomId,
			});
			client.Send(new TextMessage(message));

			if (this.OnLeave != null) {
				this.OnLeave(roomId);
			}

			this.JoinedRoom = null;
		}

		/// <summary>
		/// コメントを投稿します。
		/// </summary>
		/// <param name="name">名前</param>
		/// <param name="message">メッセージ</param>
		/// <param name="apiKey">APIキー</param>
		public void PostComment(String name, String message, String apiKey = "") {
			if (this.JoinedRoom == null) {
				throw new CavetubeException("部屋に所属していません。");
			}

			if (String.IsNullOrWhiteSpace(message)) {
				return;
			}

			var jsonString = DynamicJson.Serialize(new {
				devkey = devkey,
				mode = "post",
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
		/// <param name="commentNumber">BANするコメント番号</param>
		/// <param name="apiKey">APIキー</param>
		/// <exception cref="System.ArgumentException"></exception>
		public void BanListener(Int32 commentNumber, String apiKey) {
			if (String.IsNullOrWhiteSpace(apiKey)) {
				throw new ArgumentException("APIキーは必須です。");
			}

			if (this.JoinedRoom == null) {
				throw new CavetubeException("部屋に所属していません。");
			}

			var message = DynamicJson.Serialize(new {
				devkey = devkey,
				mode = "ban",
				commentNumber = commentNumber,
				apikey = apiKey,
			});
			client.Send(new TextMessage(message));
		}

		/// <summary>
		/// リスナーのBANを解除します。
		/// </summary>
		/// <param name="commentNumber">BAN解除するコメント番号</param>
		/// <param name="apiKey">APIキー</param>
		/// <exception cref="System.ArgumentException"></exception>
		public void UnBanListener(Int32 commentNumber, String apiKey) {
			if (String.IsNullOrWhiteSpace(apiKey)) {
				throw new ArgumentException("APIキーは必須です。");
			}

			if (this.JoinedRoom == null) {
				throw new CavetubeException("部屋に所属していません。");
			}

			var message = DynamicJson.Serialize(new {
				devkey = devkey,
				mode = "unban",
				roomId = this.JoinedRoom.RoomId,
				commentNumber = commentNumber,
				apikey = apiKey,
			});
			client.Send(new TextMessage(message));
		}

		/// <summary>
		/// 指定したコメントの非表示要求を行います。
		/// </summary>
		/// <param name="commentNumber">ID表示するコメント番号</param>
		/// <param name="apiKey">APIキー</param>
		public void HideComment(Int32 commentNumber, String apiKey) {
			if (String.IsNullOrWhiteSpace(apiKey)) {
				throw new ArgumentException("APIキーは必須です。");
			}

			if (this.JoinedRoom == null) {
				throw new CavetubeException("部屋に所属していません。");
			}

			var message = DynamicJson.Serialize(new {
				devkey = devkey,
				mode = "hide_comment",
				roomId = this.JoinedRoom.RoomId,
				commentNumber = commentNumber,
				apikey = apiKey,
			});
			client.Send(new TextMessage(message));
		}

		/// <summary>
		/// 指定したコメントの再表示要求を行います。
		/// </summary>
		/// <param name="commentNumber">ID表示するコメント番号</param>
		/// <param name="apiKey">APIキー</param>
		public void ShowComment(Int32 commentNumber, String apiKey) {
			if (String.IsNullOrWhiteSpace(apiKey)) {
				throw new ArgumentException("APIキーは必須です。");
			}

			if (this.JoinedRoom == null) {
				throw new CavetubeException("部屋に所属していません。");
			}

			var message = DynamicJson.Serialize(new {
				devkey = devkey,
				mode = "show_comment",
				roomId = this.JoinedRoom.RoomId,
				commentNumber = commentNumber,
				apikey = apiKey,
			});
			client.Send(new TextMessage(message));
		}

		/// <summary>
		/// 指定したコメント番号のリスナーのID表示要求を行います。
		/// </summary>
		/// <param name="commentNumber">ID表示するコメント番号</param>
		/// <param name="apiKey">APIキー</param>
		public void ShowId(Int32 commentNumber, String apiKey) {
			if (String.IsNullOrWhiteSpace(apiKey)) {
				throw new ArgumentException("APIキーは必須です。");
			}

			if (this.JoinedRoom == null) {
				throw new CavetubeException("部屋に所属していません。");
			}

			var message = DynamicJson.Serialize(new {
				devkey = devkey,
				mode = "show_id",
				commentNumber = commentNumber,
				apikey = apiKey,
			});
			client.Send(new TextMessage(message));
		}

		/// <summary>
		/// 指定したコメント番号のリスナーのID表示解除要求を行います。
		/// </summary>
		/// <param name="commentNumber">ID表示を解除するコメント番号</param>
		/// <param name="apiKey">APIキー</param>
		public void HideId(Int32 commentNumber, String apiKey) {
			if (String.IsNullOrWhiteSpace(apiKey)) {
				throw new ArgumentException("APIキーは必須です。");
			}

			if (this.JoinedRoom == null) {
				throw new CavetubeException("部屋に所属していません。");
			}

			var message = DynamicJson.Serialize(new {
				devkey = devkey,
				mode = "hide_id",
				roomId = this.JoinedRoom.RoomId,
				commentNumber = commentNumber,
				apikey = apiKey,
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
			this.OnMessage -= this.HandleCommentInformation;
			this.OnMessage -= this.HandleRoomInformation;
			this.OnMessage -= this.HandleLiveInfomation;
			this.OnMessage -= this.HandleOtherInformation;

			if (this.client == null) {
				return;
			}
			this.client.Dispose();
			this.client = null;

			this.connectionOpenEvent.Dispose();
		}

		/// <summary>
		/// コメントの情報を処理します。
		/// </summary>
		/// <param name="json"></param>
		private async void HandleCommentInformation(dynamic json) {
			try {
				String mode = json.mode;
				switch (mode) {
					case "ready":
						this.JoinedRoom = await this.GetSummaryAsync((String)json.room);
						if (this.OnJoin != null) {
							this.OnJoin(this.JoinedRoom.RoomId);
						}
						break;
					case "get":
						if (this.OnMessageList != null) {
							var messages = this.ParseMessage(json);
							this.OnMessageList(messages);
						}
						break;
					case "post":
						if (this.OnNewMessage != null) {
							var post = new Message(json);
							this.OnNewMessage(post);
						}
						break;
					case "post_result":
						if (this.OnPostResult != null) {
							var result = json.IsDefined("result") ? json.result : false;
							this.OnPostResult(result);
						}
						break;
					case "ban_user":
						if (this.OnBan != null) {
							var message = new Message(json);
							message.IsBan = true;
							this.OnBan(message);
						}
						break;
					case "unban_user":
						if (this.OnUnBan != null) {
							var message = new Message(json);
							this.OnUnBan(message);
						}
						break;
					case "ban_fail":
						if (this.OnBanFail != null) {
							var banFail = new BanFail(json);
							this.OnBanFail(banFail);
						}
						break;
					case "hide_comment":
						if (this.OnHideComment != null) {
							var message = new Message(json);
							this.OnHideComment(message);
						}
						break;
					case "show_comment":
						if (this.OnShowComment != null) {
							var message = new Message(json);
							this.OnShowComment(message);
						}
						break;
					case "show_id":
						if (this.OnShowId != null) {
							var idNotify = new IdNotification(json);
							this.OnShowId(idNotify);
						}
						break;
					case "hide_id":
						if (this.OnHideId != null) {
							var idNotify = new IdNotification(json);
							this.OnHideId(idNotify);
						}
						break;
					default:
						break;
				}
			} catch (CavetubeException e) {
				Debug.WriteLine(e.Message);
			} catch (XmlException) {
				Debug.WriteLine("メッセージのParseに失敗しました。");
			} catch (RuntimeBinderException) {
				Debug.WriteLine("Json内にプロパティが見つかりませんでした。");
			}
		}

		/// <summary>
		/// 部屋に関する情報を処理します。
		/// </summary>
		/// <param name="json"></param>
		private void HandleRoomInformation(dynamic json) {
			String mode = json.mode;
			switch (mode) {
				case "join":
				case "leave":
					if (this.OnUpdateMember != null && this.OnUpdateMember != null) {
						var ipCount = (Int32)json.ipcount;
						this.OnUpdateMember(ipCount);
					}
					break;
			}
		}

		/// <summary>
		/// 配信の開始/終了情報を処理します。
		/// </summary>
		/// <param name="json"></param>
		private void HandleLiveInfomation(dynamic json) {
			String mode = json.mode;
			switch (mode) {
				case "start_entry":
					if (this.OnNotifyLiveStart != null) {
						var liveInfo = new LiveNotification(json);
						this.OnNotifyLiveStart(liveInfo);
					}
					break;
				case "close_entry":
					if (this.OnNotifyLiveClose != null) {
						var liveInfo = new LiveNotification(json);
						this.OnNotifyLiveClose(liveInfo);
					}
					break;
			}
		}

		/// <summary>
		/// その他の特殊な情報を処理します。
		/// </summary>
		/// <param name="json"></param>
		private void HandleOtherInformation(dynamic json) {
			String mode = json.mode;
			switch (mode) {
				case "admin_yell":
					if (this.OnAdminShout != null) {
						var adminShout = new AdminShout(json);
						this.OnAdminShout(adminShout);
					}
					break;
			}
		}

		private IEnumerable<Message> ParseMessage(dynamic json) {
			var messages = ((dynamic[])json.comments).Select(comment => new Message(comment));
			return messages;
		}

		private async Task<String> ParseStreamUrlAsync(String url) {
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
					var jsonString = await client.DownloadStringTaskAsync(String.Format("{0}/live_url?user={1}", baseUrl, userName));
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
		public String RoomId { get; internal set; }
		public String Title { get; internal set; }
		public String Description { get; internal set; }
		public IEnumerable<String> Tags { get; internal set; }
		public Boolean IdVidible { get; internal set; }
		public Boolean AnonymousOnly { get; internal set; }
		public String Author { get; internal set; }
		public Int32 Listener { get; internal set; }
		public Int32 PageView { get; internal set; }
		public DateTime StartTime { get; internal set; }

		internal Summary() {
		}

		internal Summary(String jsonString) {
			var json = DynamicJson.Parse(jsonString);
			this.RoomId = json.IsDefined("stream_name") ? json.stream_name : String.Empty;
			this.Title = json.IsDefined("title") ? json.title : String.Empty;
			this.Description = json.IsDefined("desc") ? json.desc : String.Empty;
			this.Tags = json.IsDefined("tags") ? json.tags.Deserialize<String[]>() : new String[0];
			this.IdVidible = json.IsDefined("id_visible") ? json.id_visible : false;
			this.AnonymousOnly = json.IsDefined("anonymous_only") ? json.anonymous_only : false;
			this.Author = json.IsDefined("author") ? json.author : String.Empty;
			this.Listener = json.IsDefined("listener") ? (Int32)json.listener : 0;
			this.PageView = json.IsDefined("viewer") ? (Int32)json.viewer : 0;
			this.StartTime = json.IsDefined("start_time") ? DateExtends.ToDateTime(json.start_time) : new DateTime();
		}

		public override bool Equals(object obj) {
			var other = obj as Summary;
			if (other == null) {
				return false;
			}

			var isRoomIdSame = this.RoomId == other.RoomId;
			var isListenerSame = this.Listener == other.Listener;
			var isPageViewSame = this.PageView == other.PageView;
			return isRoomIdSame && isListenerSame && isPageViewSame;
		}

		public override int GetHashCode() {
			return this.RoomId.GetHashCode() ^ this.Listener.GetHashCode() ^ this.PageView.GetHashCode();
		}
	}

	/// <summary>
	/// コメント情報
	/// </summary>
	public class Message {
		public Int32 Number { get; internal set; }
		public String Name { get; internal set; }
		public String Comment { get; internal set; }
		public DateTime PostTime { get; internal set; }
		public String ListenerId { get; internal set; }
		public Boolean IsAuth { get; internal set; }
		public Boolean IsBan { get; internal set; }
		public Boolean IsHide { get; internal set; }

		internal Message(dynamic json) {
			this.Number = json.IsDefined("comment_num") ? (Int32)json.comment_num : 0;
			this.ListenerId = json.IsDefined("user_id") ? (String)json.user_id : String.Empty;
			this.Name = json.IsDefined("name") ? json.name : String.Empty;
			this.Comment = json.IsDefined("message") ? WebUtility.HtmlDecode(json.message) : String.Empty;
			this.IsAuth = json.IsDefined("auth") ? json.auth : false;
			this.IsBan = json.IsDefined("is_ban") ? json.is_ban : false;
			this.IsHide = json.IsDefined("is_hide") ? json.is_hide : false;
			this.PostTime = json.IsDefined("time") ? DateExtends.ToDateTime(json.time) : new DateTime();
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
	public sealed class LiveNotification {
		public String Author { get; internal set; }

		public String Title { get; internal set; }

		public String RoomId { get; internal set; }

		internal LiveNotification(dynamic json) {
			this.Author = json.IsDefined("author") ? json.author : String.Empty;
			this.Title = json.IsDefined("title") ? json.title : String.Empty;
			this.RoomId = json.IsDefined("stream_name") ? json.stream_name : String.Empty;
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

	public sealed class BanFail {
		public Int32 Number { get; internal set; }
		public String Message { get; internal set; }

		public BanFail(dynamic json) {
			this.Number = json.IsDefined("comment_num") ? json.comment_num : 0;
			this.Message = json.IsDefined("message") ? json.message : String.Empty;
		}
	}

	public sealed class IdNotification {
		public Int32 Number { get; internal set; }

		internal IdNotification(dynamic json) {
			this.Number = json.IsDefined("comment_num") ? (Int32)json.comment_num : 0;
		}
	}

	public sealed class AdminShout {
		public String Message { get; internal set; }

		internal AdminShout(dynamic json) {
			this.Message = json.IsDefined("message") ? json.message : String.Empty;
		}
	}
}