namespace CaveTube.CaveTalk.Lib {
	using System;
	using System.Linq;
	using CaveTube.CaveTubeClient;
	using System.Collections.Generic;

	public sealed class CaveTubeClientWrapper : ACommentClient {

		public override String RoomId {
			get { return this.client.JoinedRoomId; }
		}

		public override string Author {
			get { return this.client.JoinedRoomAuthor; }
		}

		public override Boolean IsConnect {
			get { return this.client.IsConnect; }
		}

		public Boolean IsDisposed { get; private set; }

		private CaveTubeClient.CavetubeClient client;

		public override event Action<IEnumerable<Message>> OnMessageList;
		public override event Action<Message> OnNewMessage;
		public override event Action<Int32> OnUpdateMember;
		public override event Action<Message> OnBan;
		public override event Action<Message> OnUnBan;
		public override event Action<String> OnJoin;
		public override event Action<String> OnLeave;
		public override event Action<Exception> OnError;

		public override void Connect() {
			try {
				this.client.Connect();
			}
			catch (CavetubeException ex) {
				throw new CommentException(ex.Message, ex);
			}
		}

		protected override Room GetRoomInfo(String url) {
			var summary = this.client.GetSummary(url);
			var messages = this.client.GetComment(url);
			return new Room {
				Summary = new Summary(summary),
				Messages = messages.Select(m => new Message(m)),
			};
		}

		public override void JoinRoom(String url) {
			this.client.JoinRoom(url);
		}

		public override void LeaveRoom() {
			this.client.LeaveRoom();
		}

		public override void BanListener(Int32 commentNumber, String apiKey) {
			this.client.BanListener(commentNumber, apiKey);
		}

		public override void UnBanListener(Int32 commentNumber, String apiKey) {
			this.client.UnBanListener(commentNumber, apiKey);
		}

		public override void PostComment(String name, String message, String apiKey) {
			this.client.PostComment(name, message, apiKey);
		}

		public CaveTubeClientWrapper()
			: this(new CavetubeClient()) {
		}

		private CaveTubeClientWrapper(CaveTubeClient.CavetubeClient client) {
			this.client = client;

			this.client.OnJoin += this.Join;
			this.client.OnLeave += this.Leave;
			this.client.OnNewMessage += this.NewMessage;
			this.client.OnUpdateMember += this.UpdateMember;
			this.client.OnBan += this.Ban;
			this.client.OnUnBan += this.UnBan;
			this.client.OnError += this.Error;
		}

		~CaveTubeClientWrapper() {
			if (this.IsDisposed == false) {
				this.Dispose();
			}
		}

		public override void Dispose() {
			this.IsDisposed = true;
			this.client.OnJoin -= this.Join;
			this.client.OnLeave -= this.Leave;
			this.client.OnNewMessage -= this.NewMessage;
			this.client.OnUpdateMember -= this.UpdateMember;
			this.client.OnBan -= this.Ban;
			this.client.OnUnBan -= this.UnBan;
			this.client.OnError -= this.Error;
			this.OnJoin = null;
			this.OnLeave = null;
			this.OnNewMessage = null;
			this.OnUpdateMember = null;
			this.OnBan = null;
			this.OnUnBan = null;
			this.OnError = null;
		}

		private void Join(String roomId) {
			if (this.OnJoin != null) {
				this.OnJoin(roomId);
			}
		}

		private void Leave(String roomId) {
			if (this.OnLeave != null) {
				this.OnLeave(roomId);
			}
		}

		public void MessageList(IEnumerable<CaveTubeClient.Message> messageList) {
			if (this.OnMessageList != null) {
				this.OnMessageList(messageList.Select(message => new Message(message)));
			}
		}

		private void NewMessage(CaveTubeClient.Message message) {
			if (this.OnNewMessage != null) {
				this.OnNewMessage(new Message(message));
			}
		}

		private void UpdateMember(Int32 count) {
			if (this.OnUpdateMember != null) {
				this.OnUpdateMember(count);
			}
		}

		private void Ban(CaveTubeClient.Message message) {
			if (this.OnBan != null) {
				this.OnBan(new Message(message));
			}
		}

		private void UnBan(CaveTubeClient.Message message) {
			if (this.OnUnBan != null) {
				this.OnUnBan(new Message(message));
			}
		}

		private void Error(CaveTubeClient.CavetubeException e) {
			if (this.OnError != null) {
				this.OnError(e);
			}
		}
	}

	public partial class Summary {
		public Summary(CaveTubeClient.Summary summary) {
			this.RoomId = summary.RoomId;
			this.Title = summary.Title;
			this.Author = summary.Author;
			this.PageView = summary.PageView;
			this.Listener = summary.Listener;
			this.StartTime = summary.StartTime;
		}
	}

	public partial class Message {
		public Message(CaveTubeClient.Message message) {
			this.IsAuth = message.Auth;
			this.Comment = message.Comment;
			this.ListenerId = message.Id;
			this.IsBan = message.IsBan;
			this.Name = message.Name;
			this.Number = message.Number;
			this.PostTime = message.Time;
		}
	}
}
