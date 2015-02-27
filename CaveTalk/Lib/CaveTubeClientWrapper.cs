namespace CaveTube.CaveTalk.Lib {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using AutoMapper;
	using CaveTube.CaveTubeClient;

	public sealed class CaveTubeClientWrapper : ACommentClient {
		static CaveTubeClientWrapper() {
			Mapper.CreateMap<CaveTubeClient.Summary, Summary>();
			Mapper.CreateMap<CaveTubeClient.Message, Message>();
			Mapper.CreateMap<CaveTubeClient.LiveNotification, LiveNotification>();
		}

		private Summary joinedRoomSummary;
		public override Summary JoinedRoomSummary {
			get {
				if (String.IsNullOrWhiteSpace(this.client.JoinedRoomId)) {
					return null;
				}
				return this.joinedRoomSummary;
			}
		}

		public override Boolean IsConnect {
			get { return this.client.IsConnect; }
		}

		public String SocketId { get { return this.client.SocketId; } }

		public Boolean IsDisposed { get; private set; }

		private CaveTubeClient.CavetubeClient client;

		public override event Action<IEnumerable<Message>> OnMessageList;
		public override event Action<Message> OnNewMessage;
		public override event Action<Int32> OnUpdateMember;
		public override event Action<Message> OnBan;
		public override event Action<Message> OnUnBan;
		public override event Action<Message> OnHideComment;
		public override event Action<Message> OnShowComment;
		public override event Action<String> OnJoin;
		public override event Action<String> OnLeave;
		public override event Action<String> OnInstantMessage;
		public override event Action<String> OnAdminShout;
		public override event Action<Exception> OnError;
		public override event Action<LiveNotification> OnNotifyLiveStart;
		public override event Action<LiveNotification> OnNotifyLiveClose;

		public override void Connect() {
			try {
				this.client.Connect();
			} catch (CavetubeException ex) {
				throw new CommentException(ex.Message, ex);
			}
		}

		protected override async Task<Room> GetRoomInfoAsync(String url) {
			try {
				var summary = Mapper.Map<Summary>(await this.client.GetSummaryAsync(url));
				var messages = (await this.client.GetCommentAsync(url)).Select(m => Mapper.Map<Message>(m));
				return new Room(summary, messages);
			} catch (CavetubeException) {
				return new Room(null, null);
			}
		}

		public override async Task JoinRoomGenAsync(String url) {
			try {
				await this.client.JoinRoomAsync(url);
				this.joinedRoomSummary = Mapper.Map<Summary>(await this.client.GetSummaryAsync(url));
			} catch (FormatException ex) {
				throw new CommentException(ex.Message, ex);
			} catch (CavetubeException ex) {
				throw new CommentException(ex.Message, ex);
			}
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

		public override void ShowId(Int32 commentNumber, String apiKey) {
			this.client.ShowId(commentNumber, apiKey);
		}

		public override void HideId(Int32 commentNumber, String apiKey) {
			this.client.HideId(commentNumber, apiKey);
		}

		public override void HideComment(Int32 commentNumber, string apiKey) {
			this.client.HideComment(commentNumber, apiKey);
		}

		public override void ShowComment(Int32 commentNumber, string apiKey) {
			this.client.ShowComment(commentNumber, apiKey);
		}

		public override void PostComment(String name, String message, String apiKey) {
			this.client.PostComment(name, message, apiKey);
		}

		public override async Task<Boolean> AllowInstantMessageAsync(Int32 commentNumber, String apiKey) {
			return await this.client.AllowInstantMessage(commentNumber, apiKey);
		}

		public CaveTubeClientWrapper(String accessKey)
			: this(new CavetubeClient(accessKey)) {
		}

		private CaveTubeClientWrapper(CaveTubeClient.CavetubeClient client) {
			this.client = client;

			this.client.OnJoin += this.Join;
			this.client.OnLeave += this.Leave;
			this.client.OnNewMessage += this.NewMessage;
			this.client.OnUpdateMember += this.UpdateMember;
			this.client.OnBan += this.Ban;
			this.client.OnUnBan += this.UnBan;
			this.client.OnHideComment += this.HideComment;
			this.client.OnShowComment += this.ShowComment;
			this.client.OnReceiveInstantMessage += this.InstantMessage;
			this.client.OnAdminShout += this.AdminShout;
			this.client.OnError += this.Error;
			this.client.OnNotifyLiveStart += this.NotifyLiveStart;
			this.client.OnNotifyLiveClose += this.NotifyLiveClose;
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
			this.client.OnHideComment -= this.HideComment;
			this.client.OnShowComment -= this.ShowComment;
			this.client.OnReceiveInstantMessage -= this.InstantMessage;
			this.client.OnAdminShout -= this.AdminShout;
			this.client.OnError -= this.Error;
			this.client.OnNotifyLiveStart -= this.NotifyLiveStart;
			this.client.OnNotifyLiveClose -= this.NotifyLiveClose;

			this.client.Dispose();
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
				this.OnMessageList(messageList.Select(message => Mapper.Map<Message>(message)));
			}
		}

		private void NewMessage(CaveTubeClient.Message message) {
			if (this.OnNewMessage != null) {
				this.OnNewMessage(Mapper.Map<Message>(message));
			}
		}

		private void UpdateMember(Int32 count) {
			if (this.OnUpdateMember != null) {
				this.OnUpdateMember(count);
			}
		}

		private void Ban(CaveTubeClient.Message message) {
			if (this.OnBan != null) {
				this.OnBan(Mapper.Map<Message>(message));
			}
		}

		private void UnBan(CaveTubeClient.Message message) {
			if (this.OnUnBan != null) {
				this.OnUnBan(Mapper.Map<Message>(message));
			}
		}

		private void HideComment(CaveTubeClient.Message message) {
			if (this.OnHideComment != null) {
				this.OnHideComment(Mapper.Map<Message>(message));
			}
		}

		private void ShowComment(CaveTubeClient.Message message) {
			if (this.OnShowComment != null) {
				this.OnShowComment(Mapper.Map<Message>(message));
			}
		}

		private void InstantMessage(String message) {
			if (this.OnInstantMessage != null) {
				this.OnInstantMessage(message);
			}
		}

		private void AdminShout(CaveTubeClient.AdminShout shout) {
			if (this.OnAdminShout != null) {
				this.OnAdminShout(shout.Message);
			}
		}

		private void Error(CaveTubeClient.CavetubeException e) {
			if (this.OnError != null) {
				this.OnError(e);
			}
		}

		private void NotifyLiveStart(CaveTubeClient.LiveNotification e) {
			if (this.OnNotifyLiveStart != null) {
				this.OnNotifyLiveStart(Mapper.Map<LiveNotification>(e));
			}
		}

		private void NotifyLiveClose(CaveTubeClient.LiveNotification e) {
			if (this.OnNotifyLiveClose != null) {
				this.OnNotifyLiveClose(Mapper.Map<LiveNotification>(e));
			}
		}
	}

	public partial class Room {
		public Room(Summary summary, IEnumerable<Message> messages) {
			this.Summary = summary;
			this.Messages = messages;
		}
	}
}
