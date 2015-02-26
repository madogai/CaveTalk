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
	using Microsoft.CSharp.RuntimeBinder;
	using Newtonsoft.Json.Linq;
	using Quobject.SocketIoClientDotNet.Client;

	public sealed class CavetubeClient : IDisposable {
		private const Int32 defaultTimeout = 3000;

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
		/// 投票が開始された時に通知されるイベントです。
		/// </summary>
		public event Action<Vote> OnVoteStart;
		/// <summary>
		/// 投票結果が開示された時に通知されるイベントです。
		/// </summary>
		public event Action<Vote> OnVoteResult;
		/// <summary>
		/// 投票がキャンセルされた時に通知されるイベントです。
		/// </summary>
		public event Action OnVoteStop;
		/// <summary>
		/// インスタントメッセージ招待を受けたときに通知されるイベントです。
		/// </summary>
		public event Action OnInviteInstantMessage;
		/// <summary>
		/// インスタントメッセージの送信を受け取った時に通知されるイベントです。
		/// </summary>
		public event Action<String> OnReceiveInstantMessage;
		/// <summary>
		/// 所属している部屋にインスタントメッセージ招待が行われた場合に通知されるイベントです。
		/// </summary>
		public event Action OnNotifyInviteInstantMessage;
		/// <summary>
		/// 所属している部屋でインスタントメッセージの送信が行われた場合に通知されるイベントです。
		/// </summary>
		public event Action OnNotifySendInstantMessage;

		/// <summary>
		/// 管理者メッセージ通知です。
		/// </summary>
		public event Action<AdminShout> OnAdminShout;
		/// <summary>
		/// 何かしらのエラーが発生したときに通知されるイベントです。
		/// </summary>
		public event Action<CavetubeException> OnError;

		/// <summary>
		/// ソケットID
		/// </summary>
		public String SocketId {
			get {
				return this.client.Io().EngineSocket.Id;
			}
		}
		/// <summary>
		/// コメントサーバとの接続状態
		/// </summary>
		public Boolean IsConnect {
			get {
				return this.client.Io().ReadyState == Manager.ReadyStateEnum.OPEN;
			}
		}
		/// <summary>
		/// 入室している部屋のサマリー
		/// </summary>
		public Summary JoinedRoom { get; private set; }

		private Uri webUri;
		private Uri socketIOUri;
		private Socket client;
		private ManualResetEvent connectionOpenEvent;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="client"></param>
		public CavetubeClient(String accessKey) {
			this.webUri = new Uri(webUrl);
			this.socketIOUri = new Uri(socketIOUrl);
			this.connectionOpenEvent = new ManualResetEvent(false);
			this.client = IO.Socket(socketIOUri, new IO.Options() {
				ReconnectionAttempts = 15,
				AutoConnect = false,
				Query = new Dictionary<String, String> {
					{"accessKey", accessKey},
				},
			});
			this.client.On(Socket.EVENT_CONNECT, async () => {
				if (this.OnConnect != null) {
					this.OnConnect();
				}

				if (this.JoinedRoom != null && String.IsNullOrWhiteSpace(this.JoinedRoom.RoomId) == false) {
					try {
						await this.JoinRoomAsync(this.JoinedRoom.RoomId);
					} catch (FormatException) {
					} catch (CavetubeException) {

					}
				}
			});

			this.client.On(Socket.EVENT_DISCONNECT, () => {
				if (this.OnDisconnect != null) {
					this.OnDisconnect();
				}
			});

			this.client.On(Socket.EVENT_ERROR, e => {
				if (this.OnError != null) {
					this.OnError(new CavetubeException("エラーが発生しました。"));
				}
			});

			#region コメント関係のハンドリング
			this.client.On("ready", this.HandleReady);
			this.client.On("get", this.HandleGetComment);
			this.client.On("post", this.HandlePostComment);
			this.client.On("post_result", this.HandlePostResult);
			this.client.On("ban_user", this.HandleBanUser);
			this.client.On("unban_user", this.HandleUnBanUser);
			this.client.On("ban_fail", this.HandleBanFail);
			this.client.On("hide_comment", this.HandleHideComment);
			this.client.On("show_comment", this.HandleShowComment);
			this.client.On("show_id", this.HandleShowId);
			this.client.On("hide_id", this.HandleHideId);

			this.client.On("invite_instant_message", this.HandleInviteInstantMessage);
			this.client.On("receive_instant_message", this.HandleReceiveInstantMessage);
			this.client.On("notify_invite_instant_message", this.HandleNotifyInviteInstantMessage);
			this.client.On("notify_send_instant_message", this.HandleNotifySendInstantMessage);
			#endregion

			# region Room関連のハンドリング
			this.client.On("join", this.HandleJoinAndLeave);
			this.client.On("leave", this.HandleJoinAndLeave);
			# endregion

			# region 投票関連のハンドリング
			this.client.On("vote_start", this.HandleVoteStart);
			this.client.On("vote_result", this.HandleVoteResult);
			this.client.On("vote_stop", this.HandleVoteStop);
			#endregion

			# region 通知関連のハンドリング
			this.client.On("start_entry", this.HandleStartEntry);
			this.client.On("close_entry", this.HandleCloseEntry);
			#endregion

			# region その他のハンドリング
			this.client.On("admin_yell", this.HandleYell);
		}
			#endregion

		~CavetubeClient() {
			this.Dispose();
		}

		/// <summary>
		/// CaveTubeコメントサーバに接続します。
		/// </summary>
		/// <exception cref="System.Net.WebException" />
		public void Connect() {
			this.connectionOpenEvent.Reset();
			this.client.Once(Socket.EVENT_CONNECT, () => {
				this.connectionOpenEvent.Set();
			});
			this.client.Open();
			var isConnect = this.connectionOpenEvent.WaitOne(3000);
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

				using (var client = WebClientUtil.CreateInstance()) {
					client.QueryString.Add("stream_name", streamName);
					client.QueryString.Add("devkey", devkey);
					var url = String.Format("{0}://{1}:{2}/api/summary", this.webUri.Scheme, this.webUri.Host, this.webUri.Port);
					var jsonString = await client.DownloadStringTaskAsync(url);
					if (String.IsNullOrEmpty(jsonString)) {
						throw new CavetubeException("サマリーの取得に失敗しました。");
					}

					dynamic json = JObject.Parse(jsonString);
					return new Summary(json);
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

				using (var client = WebClientUtil.CreateInstance()) {
					client.QueryString.Add("devkey", devkey);
					var url = String.Format("{0}://{1}:{2}/comment/{3}", this.socketIOUri.Scheme, this.socketIOUri.Host, this.socketIOUri.Port, streamName);

					var jsonString = await client.DownloadStringTaskAsync(url);
					if (String.IsNullOrEmpty(jsonString)) {
						throw new CavetubeException("コメントの取得に失敗しました。");
					}

					dynamic json = JObject.Parse(jsonString);
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
		/// <exception cref="CaveTubeException">入室に失敗しました。</exception>
		public async Task JoinRoomAsync(String liveUrl) {
			var roomId = await this.ParseStreamUrlAsync(liveUrl);
			if (String.IsNullOrWhiteSpace(roomId)) {
				throw new FormatException("URLのフォーマットが正常ではありません。");
			}

			this.connectionOpenEvent.Reset();
			this.client.Once("ready", () => {
				this.connectionOpenEvent.Set();
			});

			client.Emit("join", JObject.FromObject(new {
				devkey = devkey,
				roomId = roomId,
			}));

			var isJoin = this.connectionOpenEvent.WaitOne(3000);
			if (isJoin == false) {
				throw new CavetubeException("部屋への入室に失敗しました。");
			}
		}

		/// <summary>
		/// コメントルームから退出します。
		/// </summary>
		public void LeaveRoom() {
			if (this.JoinedRoom == null) {
				return;
			}

			var roomId = this.JoinedRoom.RoomId;
			client.Emit("leave", JObject.FromObject(new {
				devkey = devkey,
				roomId = roomId,
			}));

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

			client.Emit("post", JObject.FromObject(new {
				devkey = devkey,
				roomId = this.JoinedRoom.RoomId,
				name = name,
				message = message,
				apikey = apiKey
			}));
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

			client.Emit("ban", JObject.FromObject(new {
				devkey = devkey,
				roomId = this.JoinedRoom.RoomId,
				commentNumber = commentNumber,
				apikey = apiKey,
			}));
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

			client.Emit("unban", JObject.FromObject(new {
				devkey = devkey,
				roomId = this.JoinedRoom.RoomId,
				commentNumber = commentNumber,
				apikey = apiKey,
			}));
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

			client.Emit("hide_comment", JObject.FromObject(new {
				devkey = devkey,
				roomId = this.JoinedRoom.RoomId,
				commentNumber = commentNumber,
				apikey = apiKey,
			}));
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

			client.Emit("show_comment", JObject.FromObject(new {
				devkey = devkey,
				roomId = this.JoinedRoom.RoomId,
				commentNumber = commentNumber,
				apikey = apiKey,
			}));
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

			client.Emit("show_id", JObject.FromObject(new {
				devkey = devkey,
				roomId = this.JoinedRoom.RoomId,
				commentNumber = commentNumber,
				apikey = apiKey,
			}));
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

			client.Emit("hide_id", JObject.FromObject(new {
				devkey = devkey,
				roomId = this.JoinedRoom.RoomId,
				commentNumber = commentNumber,
				apikey = apiKey,
			}));
		}

		/// <summary>
		/// 投票を開始します。
		/// </summary>
		/// <param name="question">質問</param>
		/// <param name="choices">回答リスト</param>
		/// <param name="apiKey">APIキー</param>
		public void VoteStart(String question, IList<String> choices, String apiKey) {
			if (String.IsNullOrWhiteSpace(apiKey)) {
				throw new ArgumentException("APIキーは必須です。");
			}

			if (this.JoinedRoom == null) {
				throw new CavetubeException("部屋に所属していません。");
			}

			client.Emit("vote_start", JObject.FromObject(new {
				devkey = devkey,
				roomId = this.JoinedRoom.RoomId,
				question = question,
				choices = choices,
				apikey = apiKey,
			}));
		}

		/// <summary>
		/// 投票結果を表示します。
		/// </summary>
		/// <param name="apiKey">APIキー</param>
		public void VoteResult(String apiKey) {
			if (String.IsNullOrWhiteSpace(apiKey)) {
				throw new ArgumentException("APIキーは必須です。");
			}

			if (this.JoinedRoom == null) {
				throw new CavetubeException("部屋に所属していません。");
			}

			client.Emit("vote_result", JObject.FromObject(new {
				devkey = devkey,
				roomId = this.JoinedRoom.RoomId,
				apiKey = apiKey,
			}));
		}

		/// <summary>
		/// 投票を中断します。
		/// </summary>
		/// <param name="apiKey">APIキー</param>
		public void VoteStop(String apiKey) {
			if (String.IsNullOrWhiteSpace(apiKey)) {
				throw new ArgumentException("APIキーは必須です。");
			}

			if (this.JoinedRoom == null) {
				throw new CavetubeException("部屋に所属していません。");
			}

			client.Emit("vote_stop", JObject.FromObject(new {
				devkey = devkey,
				roomId = this.JoinedRoom.RoomId,
				apiKey = apiKey,
			}));
		}

		/// <summary>
		/// インスタントメッセージを許可します。
		/// </summary>
		/// <param name="commentNumber">コメント番号</param>
		/// <param name="apiKey">APIキー</param>
		/// <returns></returns>
		public Task<Boolean> AllowInstantMessage(Int32 commentNumber, String apiKey) {
			var tcs = new TaskCompletionSource<Boolean>();

			this.client.On("allow_instant_message", (dynamic json) => {
				Boolean isSuccess = json.result ?? false;
				tcs.TrySetResult(isSuccess);
				this.client.Off("allow_instant_message");
			});

			client.Emit("allow_instant_message", JObject.FromObject(new {
				devkey = devkey,
				roomId = this.JoinedRoom.RoomId,
				commentNumber = commentNumber,
				apikey = apiKey,
			}));

			TimerUtil.SetTimeout(defaultTimeout, () => {
				tcs.TrySetResult(false);
			});

			return tcs.Task;
		}

		/// <summary>
		/// インスタントメッセージを送信します。
		/// </summary>
		/// <param name="message">メッセージ</param>
		/// <returns></returns>
		public Task<Boolean> SendInstantMessage(String message) {
			var tcs = new TaskCompletionSource<Boolean>();

			this.client.On("send_instant_message", (dynamic json) => {
				Boolean isSuccess = json.result ?? false;
				tcs.TrySetResult(isSuccess);
				this.client.Off("allow_instant_message");
			});

			client.Emit("send_instant_message", JObject.FromObject(new {
				devkey = devkey,
				roomId = this.JoinedRoom.RoomId,
				message = message,
			}));

			TimerUtil.SetTimeout(defaultTimeout, () => {
				tcs.TrySetResult(false);
			});

			return tcs.Task;
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

			this.client.Close();
			this.client = null;

			this.connectionOpenEvent.Dispose();
		}

		#region SocketIOのイベントハンドラ
		private async void HandleReady(dynamic json) {
			this.JoinedRoom = await this.GetSummaryAsync(json.roomId.ToObject<String>());
			if (this.OnJoin != null) {
				this.OnJoin(this.JoinedRoom.RoomId);
			}
			if (this.OnJoin != null) {
				this.OnJoin(json.roomId.ToObject<String>());
			}
		}

		private void HandleGetComment(dynamic json) {
			if (this.OnMessageList != null) {
				var messages = this.ParseMessage(json);
				this.OnMessageList(messages);
			}
		}

		private void HandlePostComment(dynamic json) {
			if (this.OnNewMessage != null) {
				var post = new Message(json);
				this.OnNewMessage(post);
			}
		}

		private void HandlePostResult(dynamic json) {
			if (this.OnPostResult != null) {
				var result = json.result ?? false;
				this.OnPostResult(result);
			}
		}

		private void HandleBanUser(dynamic json) {
			if (this.OnBan != null) {
				var message = new Message(json);
				message.IsBan = true;
				this.OnBan(message);
			}
		}

		private void HandleUnBanUser(dynamic json) {
			if (this.OnUnBan != null) {
				var message = new Message(json);
				this.OnUnBan(message);
			}
		}

		private void HandleBanFail(dynamic json) {
			if (this.OnBanFail != null) {
				var banFail = new BanFail(json);
				this.OnBanFail(banFail);
			}
		}

		private void HandleHideComment(dynamic json) {
			if (this.OnHideComment != null) {
				var message = new Message(json);
				this.OnHideComment(message);
			}
		}

		private void HandleShowComment(dynamic json) {
			if (this.OnShowComment != null) {
				var message = new Message(json);
				this.OnShowComment(message);
			}
		}

		private void HandleShowId(dynamic json) {
			if (this.OnShowId != null) {
				var idNotify = new IdNotification(json);
				this.OnShowId(idNotify);
			}
		}

		private void HandleHideId(dynamic json) {
			if (this.OnHideId != null) {
				var idNotify = new IdNotification(json);
				this.OnHideId(idNotify);
			}
		}

		private void HandleInviteInstantMessage(dynamic json) {
			if (this.OnInviteInstantMessage != null) {
				this.OnInviteInstantMessage();
			}
		}

		private void HandleReceiveInstantMessage(dynamic json) {
			if (this.OnReceiveInstantMessage != null && json.message != null) {
				String message = json.message ?? String.Empty;
				this.OnReceiveInstantMessage(message);
			}
		}

		private void HandleNotifyInviteInstantMessage(dynamic json) {
			if (this.OnNotifyInviteInstantMessage != null) {
				this.OnNotifyInviteInstantMessage();
			}
		}

		private void HandleNotifySendInstantMessage(dynamic json) {
			if (this.OnNotifySendInstantMessage != null) {
				this.OnNotifySendInstantMessage();
			}
		}

		private void HandleJoinAndLeave(dynamic json) {
			if (this.OnUpdateMember != null && json.ipcount != null) {
				this.OnUpdateMember(json.ipcount.ToObject<Int32>());
			}
		}

		private void HandleVoteStart(dynamic json) {
			if (this.OnVoteStart != null) {
				var vote = new Vote(json);
				this.OnVoteStart(vote);
			}
		}

		private void HandleVoteResult(dynamic json) {
			if (this.OnVoteResult != null) {
				var vote = new Vote(json);
				this.OnVoteResult(vote);
			}
		}

		private void HandleVoteStop(dynamic json) {
			if (this.OnVoteStop != null) {
				this.OnVoteStop();
			}
		}

		private void HandleStartEntry(dynamic json) {
			if (this.OnNotifyLiveStart != null) {
				var liveInfo = new LiveNotification(json);
				this.OnNotifyLiveStart(liveInfo);
			}
		}

		private void HandleCloseEntry(dynamic json) {
			if (this.OnNotifyLiveClose != null) {
				var liveInfo = new LiveNotification(json);
				this.OnNotifyLiveClose(liveInfo);
			}
		}

		private void HandleYell(dynamic json) {
			if (this.OnAdminShout != null) {
				var adminShout = new AdminShout(json);
				this.OnAdminShout(adminShout);
			}
		}
		#endregion

		private IEnumerable<Message> ParseMessage(dynamic json) {
			var messages = ((JArray)json.comments).Select(comment => new Message(comment));
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
				using (var client = WebClientUtil.CreateInstance()) {
					var userName = match.Groups[1].Value;
					var jsonString = await client.DownloadStringTaskAsync(String.Format("{0}/api/live_url/{1}", baseUrl, userName));
					dynamic json = JObject.Parse(jsonString);
					var streamName = json.stream_name ?? String.Empty;
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

		internal Summary(dynamic json) {
			this.RoomId = json.stream_name ?? String.Empty;
			this.Title = json.title ?? String.Empty;
			this.Description = json.desc ?? String.Empty;
			this.Tags = json.tags != null ? json.tags.ToObject<String[]>() : new String[0];
			this.IdVidible = json.id_visible ?? false;
			this.AnonymousOnly = json.anonymous_only ?? false;
			this.Author = json.author ?? String.Empty;
			this.Listener = json.listener ?? 0;
			this.PageView = json.viewer ?? 0;
			this.StartTime = json.start_time != null ? DateExtends.ToDateTime(json.start_time.ToObject<Double>()) : new DateTime();
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
			this.Number = json.comment_num ?? 0;
			this.ListenerId = json.user_id ?? String.Empty;
			this.Name = json.name ?? String.Empty;
			this.Comment = json.message != null ? WebUtility.HtmlDecode(json.message.ToObject<String>()) : String.Empty;
			this.IsAuth = json.auth ?? false;
			this.IsBan = json.is_ban ?? false;
			this.IsHide = json.is_hide ?? false;
			this.PostTime = json.time != null ? DateExtends.ToDateTime(json.time.ToObject<Double>()) : new DateTime();
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
			this.Author = json.author ?? String.Empty;
			this.Title = json.title ?? String.Empty;
			this.RoomId = json.stream_name ?? String.Empty;
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
	/// BAN失敗情報
	/// </summary>
	public sealed class BanFail {
		public Int32 Number { get; internal set; }
		public String Message { get; internal set; }

		internal BanFail(dynamic json) {
			this.Number = json.comment_num ?? 0;
			this.Message = json.message ?? String.Empty;
		}
	}

	/// <summary>
	/// Id通知情報
	/// </summary>
	public sealed class IdNotification {
		public Int32 Number { get; internal set; }

		internal IdNotification(dynamic json) {
			this.Number = json.comment_num ?? 0;
		}
	}

	/// <summary>
	/// 投票情報
	/// </summary>
	public sealed class Vote {
		public String Question { get; internal set; }
		public IReadOnlyList<VoteChoice> Choices { get; internal set; }

		internal Vote(dynamic json) {
			this.Question = json.question ?? String.Empty;
			this.Choices = ((JArray)json.choices).Select((choice, i) => new VoteChoice(choice, i)).ToList();
		}
	}

	/// <summary>
	/// 投票選択肢
	/// </summary>
	public sealed class VoteChoice {
		public Int32 Number { get; internal set; }
		public String Title { get; internal set; }
		public String Result { get; internal set; }

		internal VoteChoice(dynamic json, Int32 index) {
			this.Number = index;
			this.Title = json.text ?? String.Empty;
			this.Result = json.result;
		}
	}

	/// <summary>
	/// 管理者メッセージ情報
	/// </summary>
	public sealed class AdminShout {
		public String Message { get; internal set; }

		internal AdminShout(dynamic json) {
			this.Message = json.message ?? String.Empty;
		}
	}
}