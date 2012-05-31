namespace CaveTube.CaveTalk.Lib {
	using System;
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

		public abstract String RoomId { get; }
		public abstract Boolean IsConnect { get; }

		public abstract event Action<IEnumerable<Message>> OnMessageList;
		public abstract event Action<Message> OnNewMessage;
		public abstract event Action<String> OnJoin;
		public abstract event Action<String> OnLeave;
		public abstract event Action<Int32> OnUpdateMember;
		public abstract event Action<Message> OnBan;
		public abstract event Action<Message> OnUnBan;

		/// <summary>
		/// 部屋情報を取得します。
		/// </summary>
		/// <param name="url">配信Url</param>
		/// <returns></returns>
		/// <exception cref="CaveTube.CaveTalk.Lib.CommentException" />
		public abstract Room GetRoomInfo(String url);
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
		/// コメントを投稿します。
		/// </summary>
		/// <param name="postName">名前</param>
		/// <param name="postMessage">本文</param>
		/// <param name="apiKey">APIキー</param>
		public abstract void PostComment(String postName, String postMessage, String apiKey = "");

		public abstract void Dispose();
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
