using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Net;
using CaveTube.CaveTubeClient;
using System.Windows.Media;
using NLog;
using CaveTube.CaveTalk.Lib;
using CaveTube.CaveTalk.Model;
using CaveTube.CaveTalk.Properties;

namespace CaveTube.CaveTalk.Logic {
	internal class CommentRecieveLogic {
		private Logger logger = LogManager.GetCurrentClassLogger();

		private ICommentClient commentClient;
		private CaveTalkContext context;
		private Model.Room room;

		/// <summary>
		/// メッセージをDBに保存します。
		/// </summary>
		/// <param name="messages"></param>
		private void SaveMessage(IEnumerable<Model.Message> messages) {
			var dbMessages = this.context.Messages.Where(message => message.Room.RoomId == room.RoomId);

			messages.Where(m => dbMessages.All(dm => dm.Number != m.Number && dm.PostTime != m.PostTime)).ForEach(m => {
				this.context.Messages.Add(m);
			});

			this.context.SaveChanges();
		}

		/// <summary>
		/// 接続人数やコメント一覧をクリアして初期状態に戻します。
		/// </summary>
		private void ResetStatus() {
			this.Listener = 0;
			this.MessageList.Clear();
		}

		/// <summary>
		/// コメント部屋に接続します。
		/// </summary>
		/// <param name="liveUrl"></param>
		private void JoinRoom(String liveUrl) {
			if (String.IsNullOrWhiteSpace(liveUrl)) {
				return;
			}

			this.LeaveRoom();

			if (this.commentClient != null) {
				this.commentClient.Dispose();
			}

			var urlType = this.JudgeUrl(liveUrl);
			switch (urlType) {
				case UrlType.Cavetube:
					this.commentClient = new CaveTubeClientWrapper(this.cavetubeClient);
					break;
				case UrlType.Jbbs:
					this.commentClient = null;
					//var match = Regex.Match(liveUrl, @"^http://jbbs.livedoor.jp/bbs/read.cgi/([a-z]+/\d+/\d+)");
					//if (match.Success == false) {
					//    return;
					//}

					//this.LiveUrl = match.Groups[1].Value;
					break;
				default:
					this.commentClient = null;
					break;
			}

			if (this.commentClient == null) {
				MessageBox.Show("不正なURLです。");
				return;
			}

			try {
				var room = this.commentClient.GetRoomInfo(liveUrl);
				var roomId = room.Summary.RoomId;
				if (String.IsNullOrWhiteSpace(roomId)) {
					MessageBox.Show("不正なURLです。");
					return;
				}

				var dbRoom = this.context.Rooms.Find(roomId);
				if (dbRoom == null) {
					dbRoom = new Model.Room {
						RoomId = room.Summary.RoomId,
						Title = room.Summary.Title,
						Author = room.Summary.Author,
						StartTime = room.Summary.StartTime,
					};
					this.context.Rooms.Add(dbRoom);
					this.context.SaveChanges();
				}

				this.commentClient.OnJoin += this.OnJoin;
				this.commentClient.OnMessage += this.OnReceiveMessage;
				this.commentClient.OnUpdateMember += this.OnUpdateMember;
				this.commentClient.OnBan += this.OnBanUser;
				this.commentClient.OnUnBan += this.OnUnBanUser;

				if (String.IsNullOrWhiteSpace(roomId) == false) {
					this.LiveUrl = roomId;
				}

				this.room = dbRoom;

				var dbMessages = room.Messages.Select(this.ConvertMessage);

				// DBに保存
				this.SaveMessage(dbMessages);

				// ビューモデルを追加
				var messages = dbMessages.Select(m => new Message(m, this.BanUser, this.UnBanUser, this.MarkListener));
				foreach (var message in messages) {
					this.MessageList.Insert(0, message);
				}

				try {
					this.commentClient.JoinRoom(roomId);
				} catch (WebException) {
					MessageBox.Show("コメントサーバに接続できませんでした。");
					return;
				}

			} catch (CavetubeException e) {
				MessageBox.Show(e.Message);
				logger.Error(e);
				return;
			}
		}

		/// <summary>
		/// コメント部屋から抜けます。
		/// </summary>
		private void LeaveRoom() {
			if (this.commentClient == null) {
				return;
			}

			var roomId = this.commentClient.RoomId;
			if (String.IsNullOrEmpty(this.commentClient.RoomId)) {
				return;
			}

			this.commentClient.LeaveRoom();

			base.OnPropertyChanged("RoomJoinStatus");
			this.ResetStatus();
		}

		/// <summary>
		/// コメントを投稿します。
		/// </summary>
		/// <param name="postName"></param>
		/// <param name="postMessage"></param>
		/// <param name="apiKey"></param>
		private void PostComment(String postName, String postMessage, String apiKey) {
			if (String.IsNullOrEmpty(this.commentClient.RoomId)) {
				return;
			}

			this.commentClient.PostComment(postName, postMessage, apiKey);
			this.PostMessage = String.Empty;
		}

		/// <summary>
		/// ユーザーをBANします。
		/// </summary>
		/// <param name="commentNum"></param>
		private void BanUser(Int32 commentNum) {
			if (this.LoginStatus == false) {
				MessageBox.Show("BANするにはログインが必須です。");
				return;
			}

			if (Settings.Default.UserId != this.room.Author) {
				MessageBox.Show("配信者でないとBANすることはできません。");
				return;
			}

			try {
				var isSuccess = this.commentClient.BanListener(commentNum, Settings.Default.ApiKey);
				if (isSuccess == false) {
					MessageBox.Show("BANに失敗しました。");
				}
			} catch (ArgumentException) {
				logger.Error("未ログイン状態のため、BANできませんでした。");
			} catch (WebException) {
				logger.Error("CaveTubeとの通信に失敗しました。");
				MessageBox.Show("BANに失敗しました。");
			}
		}

		/// <summary>
		/// ユーザーBANを解除します。
		/// </summary>
		/// <param name="commentNum"></param>
		private void UnBanUser(Int32 commentNum) {
			if (this.LoginStatus == false) {
				MessageBox.Show("BANするにはログインが必須です。");
				return;
			}

			if (Settings.Default.UserId != this.room.Author) {
				MessageBox.Show("配信者でないとBANすることはできません。");
				return;
			}

			try {
				var isSuccess = this.commentClient.UnBanListener(commentNum, Settings.Default.ApiKey);
				if (isSuccess == false) {
					MessageBox.Show("BANに失敗しました。");
				}
			} catch (ArgumentException) {
				logger.Error("未ログイン状態のため、BAN解除できませんでした。");
			} catch (WebException) {
				logger.Error("CaveTubeとの通信に失敗しました。");
				MessageBox.Show("BAN解除に失敗しました。");
			}
		}

		/// <summary>
		/// Id付きのリスナーに色を付けます。
		/// </summary>
		/// <param name="id"></param>
		private void MarkListener(Int32 commentNum, String id) {
			var comment = this.MessageList.FirstOrDefault(m => m.Number == commentNum);
			if (comment == null) {
				return;
			}

			var solidBrush = comment.Color as SolidColorBrush;
			if (solidBrush.Color != Colors.White) {
				comment.Color = Brushes.White;
			} else {
				var random = new Random();
				// 暗い色だと文字が見えなくなるので、96以上とします。
				var red = (byte)random.Next(96, 255);
				var green = (byte)random.Next(96, 255);
				var blue = (byte)random.Next(96, 255);
				comment.Color = new SolidColorBrush(Color.FromRgb(red, green, blue));
			}

			this.context.SaveChanges();

			this.MessageList.Refresh();
		}

		/// <summary>
		/// CavetubeClientから受け取ったメッセージをモデルに変換します。<br />
		/// 必要に応じてリスナー登録も行います。
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		private Model.Message ConvertMessage(Lib.Message message) {
			var listener = this.context.Listener.Find(message.Id);

			if (message.Id != null) {
				var account = message.Auth ? this.context.Account.Find(message.Name) : null;

				// リスナーの登録
				if (listener == null) {
					this.context.Listener.Add(new Model.Listener {
						ListenerId = message.Id,
						Name = message.Name,
						Author = this.room.Author,
						BackgroundColor = account != null ? account.BackgroundColor : Brushes.White,
						Account = account,
					});
					this.context.SaveChanges();
				} else if (message.Auth && listener.Account == null) {
					listener.Account = account ?? new Model.Account {
						AccountName = message.Name,
						BackgroundColor = Brushes.White,
					};

					// アカウントの登録
					this.context.Listener.Where(l => l.ListenerId == listener.ListenerId).ForEach(l => {
						l.Account = account;
						l.Color = account.Color;
					});

					this.context.SaveChanges();
				}
			}

			var dbMessage = new Model.Message {
				Room = this.room,
				Number = message.Number,
				Name = message.Name,
				Comment = message.Comment,
				PostTime = message.Time,
				IsBan = message.IsBan,
				IsAuth = message.Auth,
				Listener = listener,
			};
			return dbMessage;
		}

	}
}
