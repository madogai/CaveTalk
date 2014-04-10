namespace CaveTube.CaveTubeClient {
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Configuration;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Text;
	using System.Threading.Tasks;
	using System.Xml;
	using Codeplex.Data;

	public static class CaveTubeEntry {
		private static String webUrl = ConfigurationManager.AppSettings["web_server"] ?? "http://gae.cavelis.net";
		private static String devkey = ConfigurationManager.AppSettings["dev_key"] ?? String.Empty;

		/// <summary>
		/// 配信開始リクエストを行います。
		/// </summary>
		/// <param name="title">タイトル</param>
		/// <param name="apiKey">APIキー</param>
		/// <param name="description">配信詳細</param>
		/// <param name="tags">タグ</param>
		/// <param name="thumbnailSlot">サムネイルスロット</param>
		/// <param name="idVisible">ID表示の有無</param>
		/// <param name="anonymousOnly">ハンドルネーム制限</param>
		/// <param name="loginOnly">書き込み制限</param>
		/// <param name="testMode">テストモード</param>
		/// <param name="socketId">SocketIOの接続ID</param>
		/// <returns></returns>
		public static async Task<StartInfo> RequestStartBroadcastAsync(String title, String apiKey, String description, IEnumerable<String> tags, Int32 thumbnailSlot, Boolean idVisible, Boolean anonymousOnly, Boolean loginOnly, Boolean testMode, String socketId) {
			try {
				using (var client = new WebClient()) {
					var data = new NameValueCollection {
						{"devkey", devkey},
						{"apikey", apiKey},
						{"title", title},
						{"description", description},
						{"tag", String.Join(" ", tags)},
						{"thumbnail_slot", thumbnailSlot.ToString()},
						{"id_visible", idVisible ? "true" : "false"},
						{"anonymous_only", anonymousOnly ? "true" : "false"},
						{"login_only", loginOnly ? "true" : "false"},
						{"test_mode", testMode ? "true" : "false"},
						{"socket_id", socketId},
					};

					var response = await client.UploadValuesTaskAsync(String.Format("{0}/api/start", webUrl), "POST", data);
					var jsonString = Encoding.UTF8.GetString(response);

					var json = DynamicJson.Parse(jsonString);
					if (json.IsDefined("stream_name") == false) {
						return null;
					}

					return new StartInfo(json);
				}
			} catch (WebException) {
				return null;
			}
		}

		/// <summary>
		/// ユーザー登録情報を取得します。
		/// </summary>
		/// <param name="apiKey">APIキー</param>
		/// <returns></returns>
		public static async Task<UserData> RequestUserDataAsync(String apiKey) {
			try {
				using (var client = new WebClient()) {
					client.Encoding = Encoding.UTF8;

					var jsonString = await client.DownloadStringTaskAsync(String.Format("{0}/api/user_data?devkey={1}&apikey={2}", webUrl, devkey, apiKey));
					var json = DynamicJson.Parse(jsonString);
					return new UserData(json);
				}
			} catch (WebException) {
				return new UserData();
			} catch (XmlException) {
				return new UserData();
			}
		}

		/// <summary>
		/// ジャンル一覧を取得します。
		/// </summary>
		/// <param name="apiKey">APIキー</param>
		/// <returns></returns>
		public static async Task<IEnumerable<Genre>> RequestGenre(String apiKey) {
			try {
				using (var client = new WebClient()) {
					client.Encoding = Encoding.UTF8;

					var jsonString = await client.DownloadStringTaskAsync(String.Format("{0}/api/genre?devkey={1}&apikey={2}", webUrl, devkey, apiKey));
					var json = DynamicJson.Parse(jsonString);
					return ((dynamic[])json.genres).Select(genre => new Genre(genre));
				}
			} catch (WebException) {
				return Enumerable.Empty<Genre>();
			} catch (XmlException) {
				return Enumerable.Empty<Genre>();
			}
		}

		public sealed class UserData {
			public IEnumerable<Thumbnail> Thumbnails { get; private set; }

			internal UserData() {
				this.Thumbnails = Enumerable.Empty<Thumbnail>();
			}

			internal UserData(dynamic json) {
				this.Thumbnails = ((dynamic[])json.thumbnails).Select(t => new Thumbnail(t));
			}
		}

		public sealed class Genre {
			public String Title { get; private set; }
			public IEnumerable<String> Tags { get; private set; }

			internal Genre(dynamic json) {
				this.Title = json.title;
				this.Tags = ((dynamic[])json.tags).Select(t => (String)t);
			}
		}

		public sealed class Thumbnail {
			public Int32 Slot { get; private set; }
			public String Url { get; private set; }

			internal Thumbnail(dynamic json) {
				this.Slot = (Int32)json.slot;
				this.Url = json.url;
			}
		}

		public sealed class StartInfo {
			public String StreamName { get; private set; }
			public String WarnMessage { get; private set; }

			internal StartInfo(dynamic json) {
				this.StreamName = json.stream_name;
				this.WarnMessage = json.IsDefined("warn_message") ? json.warn_message : String.Empty;
			}
		}
	}
}
