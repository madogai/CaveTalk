namespace CaveTube.CaveTalk.Lib {
	using System;
	using System.Linq;
	using CaveTube.CaveTubeClient;

	public sealed class CaveTubeClientWrapper : ICommentClient {

		public String RoomId {
			get {
				return this.client.JoinedRoomId;
			}
		}

		public Boolean IsDisposed { get; private set; }

		private CaveTubeClient.CavetubeClient client;

		#region ICommentClient メンバー

		public event Action<Message> OnMessage;
		public event Action<Int32> OnUpdateMember;
		public event Action<Message> OnBan;
		public event Action<Message> OnUnBan;
		public event Action<String> OnJoin;
		public event Action<String> OnLeave;

		public Room GetRoomInfo(String url) {
			var tuple = this.client.GetCavetubeInfomation(url);
			return new Room {
				Summary = new Summary {
					RoomId = tuple.Item1.RoomId,
					Title = tuple.Item1.Title,
					Author = tuple.Item1.Author,
					PageView = tuple.Item1.PageView,
					Listener = tuple.Item1.Listener,
					StartTime = tuple.Item1.StartTime,
				},
				Messages = tuple.Item2.Select(m => new Message {
					Auth = m.Auth,
					Comment = m.Comment,
					Id = m.Id,
					IsBan = m.IsBan,
					Name = m.Name,
					Number = m.Number,
					Time = m.Time,
				}),
			};
		}

		public void JoinRoom(String roomId) {
			this.client.JoinRoom(roomId);
		}

		public void LeaveRoom() {
			this.client.LeaveRoom();
		}

		public Boolean BanListener(Int32 commentNumber, String apiKey) {
			return this.client.BanListener(commentNumber, apiKey);
		}

		public Boolean UnBanListener(Int32 commentNumber, String apiKey) {
			return this.client.UnBanListener(commentNumber, apiKey);
		}

		public void PostComment(String name, String message, String apiKey) {
			this.client.PostComment(name, message, apiKey);
		}

		#endregion

		public CaveTubeClientWrapper(CaveTubeClient.CavetubeClient client) {
			this.client = client;

			this.client.OnJoin += this.Join;
			this.client.OnLeave += this.Leave;
			this.client.OnMessage += this.Message;
			this.client.OnUpdateMember += this.UpdateMember;
			this.client.OnBan += this.Ban;
			this.client.OnUnBan += this.UnBan;
		}

		~CaveTubeClientWrapper() {
			if (this.IsDisposed == false) {
				this.Dispose();
			}
		}

		public void Dispose() {
			this.IsDisposed = true;
			this.client.OnJoin -= this.Join;
			this.client.OnLeave -= this.Leave;
			this.client.OnMessage -= this.Message;
			this.client.OnUpdateMember -= this.UpdateMember;
			this.client.OnBan -= this.Ban;
			this.client.OnUnBan -= this.UnBan;
			this.OnJoin = null;
			this.OnLeave = null;
			this.OnMessage = null;
			this.OnUpdateMember = null;
			this.OnBan = null;
			this.OnUnBan = null;
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

		private void Message(CaveTubeClient.Message m) {
			if (this.OnMessage != null) {
				this.OnMessage(new Message(m));
			}
		}

		private void UpdateMember(Int32 count) {
			if (this.OnUpdateMember != null) {
				this.OnUpdateMember(count);
			}
		}

		private void Ban(CaveTubeClient.Message m) {
			if (this.OnBan != null) {
				this.OnBan(new Message(m));
			}
		}

		private void UnBan(CaveTubeClient.Message m) {
			if (this.OnUnBan != null) {
				this.OnUnBan(new Message(m));
			}
		}
	}

	public partial class Message {
		public Message(CaveTubeClient.Message message) {
			this.Auth = message.Auth;
			this.Comment = message.Comment;
			this.Id = message.Id;
			this.IsBan = message.IsBan;
			this.Name = message.Name;
			this.Number = message.Number;
			this.Time = message.Time;
		}
	}
}
