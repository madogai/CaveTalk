namespace CaveTube.CaveTalk.Lib {
	using System;
	using System.Collections.Generic;
	using System.Text.RegularExpressions;

	public interface ICommentClient : IDisposable {
		String RoomId { get; }

		event Action<Message> OnMessage;
		event Action<String> OnJoin;
		event Action<String> OnLeave;
		event Action<Int32> OnUpdateMember;
		event Action<Message> OnBan;
		event Action<Message> OnUnBan;

		/// <summary>
		/// 部屋情報と現在のコメント一覧を取得します。
		/// </summary>
		/// <param name="url">URL</param>
		/// <returns></returns>
		/// <exception cref="CaveTube.CaveTalk.Lib.CommentException" />
		Room GetRoomInfo(String url);
		/// <summary>
		/// 部屋に入室します。
		/// </summary>
		/// <param name="roomId">ルームID</param>
		void JoinRoom(String roomId);
		/// <summary>
		/// 部屋から退出します。
		/// </summary>
		void LeaveRoom();
		/// <summary>
		/// リスナーをBANします。
		/// </summary>
		/// <param name="commentNumber"></param>
		/// <param name="apiKey"></param>
		/// <returns></returns>
		Boolean BanListener(Int32 commentNumber, String apiKey);
		/// <summary>
		/// リスナーのBANを解除します。
		/// </summary>
		/// <param name="commentNumber"></param>
		/// <param name="apiKey"></param>
		/// <returns></returns>
		Boolean UnBanListener(Int32 commentNumber, String apiKey);

		void PostComment(String postName, String postMessage, String apiKey);
	}

	public partial class Room {
		public Summary Summary { get; set; }
		public IEnumerable<Message> Messages { get; set; }
	}

	public partial class Summary {
		public String RoomId { get; set; }
		public String Title { get; set; }
		public String Author { get; set; }
		public Int32 Listener { get; set; }
		public Int32 PageView { get; set; }
		public DateTime StartTime { get; set; }
	}

	public partial class Message {
		public Int32 Number { get; set; }
		public String Id { get; set; }
		public String Name { get; set; }
		public String Comment { get; set; }
		public DateTime Time { get; set; }
		public Boolean Auth { get; set; }
		public Boolean IsBan { get; set; }

		public Message() {}
	}

	public sealed class CommentException : Exception {
		public CommentException() : base() {}
		public CommentException(String message) : base(message) {}
		public CommentException(String message, Exception innerException) : base(message, innerException) {}
	}
}
