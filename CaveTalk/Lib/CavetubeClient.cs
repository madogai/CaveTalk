namespace CaveTalk {

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Text;
	using CaveTalk.Utils;
	using Codeplex.Data;
	using SocketIO;

	public sealed class CavetubeClient : IDisposable {
		private const String socketBase = "http://ws.cavelis.net:3000/socket.io/1/";
		private const String restBase = "http://gae.cavelis.net/viewedit/getcomment?stream_name={0}&comment_num=1";

		public event Action<Object, Summary, Message> OnMessage;
		public event Action<Summary, IEnumerable<Message>> OnConnect;

		private String roomId;
		private SocketIOClient client;

		public CavetubeClient() {
			var uri = new Uri(socketBase);
			this.client = new SocketIOClient(uri);
			this.client.OnOpen += (sender, e) => {
				var msg = DynamicJson.Serialize(new {
					mode = "join",
					room = this.roomId,
				});
				client.Send(msg);
			};

			this.client.OnMessage += (sender, message) => {
				if (this.OnMessage == null) {
					return;
				}

				var json = DynamicJson.Parse(message);
				if (json.mode != "post") {
					return;
				}

				var summary = new Summary {
					Listener = (Int32)json.listener,
					PageView = (Int32)json.viewer,
				};

				var post = new Message {
					Number = (Int32)json.comment_num,
					Name = json.name,
					Comment = json.message,
					Time = JavaScriptTime.ToDateTime(json.time, "Tokyo Standard Time"),
				};

				this.OnMessage(sender, summary, post);
			};
		}

		public void Connect(String roomId) {
			this.roomId = roomId;

			var jsonString = this.GetCavetubeInfomation(roomId);
			if (String.IsNullOrEmpty(jsonString) == false) {
				var summary = this.ParseSummary(jsonString);
				var messages = this.ParseMessage(jsonString);
				if (this.OnConnect != null) {
					this.OnConnect(summary, messages);
				}
			}

			client.Connect();
		}

		public void Close() {
			if (this.client == null) {
				return;
			}
			this.client.Close();
		}

		public void Dispose() {
			if (this.client == null) {
				return;
			}
			this.client.Dispose();
			this.client = null;
		}

		~CavetubeClient() {
			this.Dispose();
		}

		private String GetCavetubeInfomation(String roomId) {
			try {
				using (var client = new WebClient()) {
					client.Encoding = Encoding.UTF8;
					var url = String.Format(restBase, roomId);
					var jsonString = client.DownloadString(url);
					var json = DynamicJson.Parse(jsonString);
					if (json.ret == false) {
						throw new WebException();
					}
					return jsonString;
				}
			} catch (WebException) {
				return String.Empty;
			}
		}

		private Summary ParseSummary(String jsonString) {
			var json = DynamicJson.Parse(jsonString);
			return new Summary {
				Listener = (Int32)json.listener,
				PageView = (Int32)json.viewer,
			};
		}

		private IEnumerable<Message> ParseMessage(String jsonString) {
			var json = DynamicJson.Parse(jsonString);
			var commentCount = json.IsDefined("comment_num") ? (Int32)json.comment_num : 0;
			var messages = Enumerable.Range(1, commentCount).Where(num => {
				var attr = String.Format("num_{0}", num);
				return json.IsDefined(attr);
			}).Select(num => {
				var attr = String.Format("num_{0}", num);
				var comment = json[attr];
				return new Message {
					Number = num,
					Name = comment.name,
					Comment = comment.message,
					Time = JavaScriptTime.ToDateTime(comment.time, "Tokyo Standard Time"),
				};
			});
			return messages;
		}
	}

	public sealed class Summary {

		public Int32 Listener { get; set; }

		public Int32 PageView { get; set; }
	}

	public sealed class Message {

		public Int32 Number { get; set; }

		public String Name { get; set; }

		public String Comment { get; set; }

		public DateTime Time { get; set; }
	}
}