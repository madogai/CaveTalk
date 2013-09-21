namespace CaveTube.CaveTalk.Lib {
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Text.RegularExpressions;

	public abstract class ACommentClient : IDisposable {
		public static ACommentClient CreateInstance(String url) {
			var webServer = ConfigurationManager.AppSettings["web_server"];
			if (Regex.IsMatch(url, String.Format(@"^(?:{0}(?:\:\d{{1,5}})?/[a-z]+/)?(?:[0-9A-Z]{{32}})", webServer))) {
				return new CaveTubeClientWrapper();
			}

			if (Regex.IsMatch(url, String.Format(@"^{0}(?:\:\d{{1,5}})?/live/(?:.*)", webServer))) {
				return new CaveTubeClientWrapper();
			}

			if (Regex.IsMatch(url, String.Format(@"^http://jbbs.livedoor.jp/bbs/read.cgi/[a-z0-9]+/\d+$"))) {
				return null;
			}

			return null;
		}

		public abstract Summary JoinedRoomSummary { get; }
		public abstract Boolean IsConnect { get; }

		public abstract event Action<IEnumerable<Message>> OnMessageList;
		public abstract event Action<Message> OnNewMessage;
		public abstract event Action<String> OnJoin;
		public abstract event Action<String> OnLeave;
		public abstract event Action<Int32> OnUpdateMember;
		public abstract event Action<Message> OnBan;
		public abstract event Action<Message> OnUnBan;
		public abstract event Action<Message> OnHideComment;
		public abstract event Action<Message> OnShowComment;
		public abstract event Action<String> OnAdminShout;
		public abstract event Action<Exception> OnError;
		public abstract event Action<LiveNotification> OnNotifyLiveStart;
		public abstract event Action<LiveNotification> OnNotifyLiveClose;

		public ACommentClient() {
			this.OnNewMessage += this.NewMessage;
		}

		~ACommentClient() {
			this.OnNewMessage -= this.NewMessage;
		}

		/// <summary>
		/// メッセージの待機を開始します。
		/// </summary>
		public abstract void Connect();
		/// <summary>
		/// 部屋情報を取得します。
		/// </summary>
		/// <param name="url">配信Url</param>
		/// <returns></returns>
		/// <exception cref="CaveTube.CaveTalk.Lib.CommentException" />
		public Room GetRoom(String url) {
			var room = this.GetRoomInfo(url);

			if (room.Summary == null) {
				return null;
			}

			var summary = room.Summary;

			// DBの部屋情報を更新します。
			var dbRoom = new Model.Room {
				RoomId = summary.RoomId,
				Title = summary.Title,
				Description = summary.Description,
				Tags = String.Join(" ", summary.Tags),
				IdVisible = summary.IdVidible,
				AnonymousOnly = summary.AnonymousOnly,
				Author = summary.Author,
				StartTime = summary.StartTime,
			};
			Model.Room.UpdateRoom(dbRoom);

			// コメントをDBに保存します。
			this.NewMessage(room.Summary, room.Messages);

			return room;
		}
		protected abstract Room GetRoomInfo(String url);

		/// <summary>
		/// 部屋に入室します。
		/// </summary>
		/// <param name="url">配信Url</param>
		public abstract void JoinRoom(String url);
		/// <summary>
		/// 部屋から退出します。
		/// </summary>
		public abstract void LeaveRoom();
		/// <summary>
		/// リスナーをBANします。
		/// </summary>
		/// <param name="commentNumber"></param>
		/// <param name="apiKey"></param>
		/// <returns></returns>
		public abstract void BanListener(Int32 commentNumber, String apiKey);
		/// <summary>
		/// リスナーのBANを解除します。
		/// </summary>
		/// <param name="commentNumber"></param>
		/// <param name="apiKey"></param>
		/// <returns></returns>
		public abstract void UnBanListener(Int32 commentNumber, String apiKey);

		/// <summary>
		/// リスナーの強制ID表示を有効にします。
		/// </summary>
		/// <param name="commentNumber"></param>
		/// <param name="apiKey"></param>
		public abstract void ShowId(Int32 commentNumber, String apiKey);

		/// <summary>
		/// リスナーの強制ID表示を解除します。
		/// </summary>
		/// <param name="commentNumber"></param>
		/// <param name="apiKey"></param>
		public abstract void HideId(Int32 commentNumber, String apiKey);

		/// <summary>
		/// コメントを非表示にします。
		/// </summary>
		/// <param name="commentNumber"></param>
		/// <param name="apiKey"></param>
		public abstract void HideComment(Int32 commentNumber, String apiKey);

		/// <summary>
		/// コメントを再表示します。
		/// </summary>
		/// <param name="commentNumber"></param>
		/// <param name="apiKey"></param>
		public abstract void ShowComment(Int32 commentNumber, String apiKey);

		/// <summary>
		/// コメントを投稿します。
		/// </summary>
		/// <param name="postName">名前</param>
		/// <param name="postMessage">本文</param>
		/// <param name="apiKey">APIキー</param>
		public abstract void PostComment(String postName, String postMessage, String apiKey = "");

		public abstract void Dispose();

		private void NewMessage(Message message) {
			if (this.JoinedRoomSummary == null) {
				return;
			}

			var author = this.JoinedRoomSummary.Author;
			if (String.IsNullOrWhiteSpace(author) == true) {
				return;
			}

			if (message.IsAuth) {
				// アカウント情報を更新
				var account = Model.Account.GetAccount(message.Name);
				if (message.IsAuth && String.IsNullOrWhiteSpace(message.Name) == false) {
					account = new Model.Account {
						AccountName = message.Name,
					};
					Model.Account.UpdateAccount(account);
				}
			}

			if (String.IsNullOrWhiteSpace(message.ListenerId) == false) {
				// リスナーが存在しなければ追加します。
				var listener = Model.Listener.GetListener(message.ListenerId);
				if (listener == null || (String.IsNullOrWhiteSpace(listener.ListenerId) == false && message.IsAuth)) {
					listener = new Model.Listener {
						ListenerId = message.ListenerId,
						Name = String.IsNullOrWhiteSpace(message.Name) == false ? message.Name : null,
						Author = author,
						AccountName = message.IsAuth ? message.Name : null,
					};
					Model.Listener.UpdateListener(listener);
				}
			}
		}

		/// <summary>
		/// GetRoomからのみ呼ばれる想定です。
		/// </summary>
		/// <param name="summary"></param>
		/// <param name="messages"></param>
		private void NewMessage(Summary summary, IEnumerable<Message> messages) {
			foreach (var message in messages) {
				if (message.IsAuth && String.IsNullOrWhiteSpace(message.Name) == false) {
					// アカウント情報を更新
					var account = Model.Account.GetAccount(message.Name);
					if (account == null) {
						account = new Model.Account {
							AccountName = message.Name,
						};
						Model.Account.UpdateAccount(account);
					}
				}

				if (String.IsNullOrWhiteSpace(message.ListenerId) == false) {
					// リスナーが存在しなければ追加します。
					var listener = Model.Listener.GetListener(message.ListenerId);
					if (listener == null || (String.IsNullOrWhiteSpace(listener.ListenerId) == false && message.IsAuth)) {
						listener = new Model.Listener {
							ListenerId = message.ListenerId,
							Name = String.IsNullOrWhiteSpace(message.Name) == false ? message.Name : null,
							Author = summary.Author,
							AccountName = message.IsAuth ? message.Name : null,
						};
						Model.Listener.UpdateListener(listener);
					}
				}
			}

			// DBのコメント情報を更新します。
			var dbMessage = messages.Select(message => new Model.Message {
				RoomId = summary.RoomId,
				Number = message.Number,
				Name = message.Name,
				Comment = message.Comment,
				IsAuth = message.IsAuth,
				IsBan = message.IsBan,
				PostTime = message.PostTime,
				ListenerId = message.ListenerId,
			});
			Model.Message.UpdateMessage(dbMessage);
		}
	}

	public partial class Room {
		public Summary Summary { get; protected set; }
		public IEnumerable<Message> Messages { get; protected set; }
	}

	public partial class Summary {
		public String RoomId { get; protected set; }
		public String Title { get; protected set; }
		public String Description { get; protected set; }
		public IEnumerable<String> Tags { get; protected set; }
		public Boolean IdVidible { get; protected set; }
		public Boolean AnonymousOnly { get; protected set; }
		public String Author { get; protected set; }
		public Int32 Listener { get; protected set; }
		public Int32 PageView { get; protected set; }
		public DateTime StartTime { get; protected set; }
	}

	public partial class Message {
		public Int32 Number { get; protected set; }
		public String ListenerId { get; protected set; }
		public String Name { get; protected set; }
		public String Comment { get; protected set; }
		public DateTime PostTime { get; protected set; }
		public Boolean IsAuth { get; protected set; }
		public Boolean IsBan { get; protected set; }
		public Boolean IsHide { get; protected set; }
		public Boolean IsAsciiArt {
			get { return Regex.IsMatch(this.Comment, "　 (?!<br>|$)"); }
		}
	}

	public partial class LiveNotification {
		public String Author { get; set; }
		public String Title { get; set; }
		public String RoomId { get; set; }
	}

	[Serializable]
	public sealed class CommentException : Exception {
		public CommentException() : base() { }
		public CommentException(String message) : base(message) { }
		public CommentException(String message, Exception innerException) : base(message, innerException) { }
	}
}
