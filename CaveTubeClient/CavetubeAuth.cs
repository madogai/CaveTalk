namespace CaveTube.CaveTubeClient {
	using System;
	using System.Collections.Specialized;
	using System.Configuration;
	using System.Net;
	using System.Text;
	using System.Threading.Tasks;
	using Newtonsoft.Json.Linq;

	public static class CavetubeAuth {
		private static String webUrl = ConfigurationManager.AppSettings["web_server"] ?? "http://gae.cavelis.net";
		private static String socketIOUrl = ConfigurationManager.AppSettings["comment_server"] ?? "http://ws.cavelis.net:3000";
		private static String devkey = ConfigurationManager.AppSettings["dev_key"] ?? String.Empty;

		/// <summary>
		/// CaveTubeにログインします。
		/// </summary>
		/// <param name="userId">ユーザー名</param>
		/// <param name="password">パスワード</param>
		/// <returns>APIキー</returns>
		/// <exception cref="System.ArgumentException" />
		/// <exception cref="System.Net.WebException" />
		public static async Task<String> LoginAsync(String userId, String password) {
			if (String.IsNullOrWhiteSpace(userId)) {
				throw new ArgumentException("UserIdが指定されていません。");
			}

			if (String.IsNullOrWhiteSpace(password)) {
				throw new ArgumentException("Passwordが指定されていません。");
			}

			using (var client = new WebClient()) {
				var data = new NameValueCollection {
					{"devkey", devkey},
					{"mode", "login"},
					{"user", userId},
					{"pass", password},
				};

				try {
					var response = await client.UploadValuesTaskAsync(String.Format("{0}/api/auth", webUrl), "POST", data);
					var jsonString = Encoding.UTF8.GetString(response);

					dynamic json = JObject.Parse(jsonString);
					if (json.apikey == null) {
						return String.Empty;
					}

					return json.apikey;
				} catch (WebException) {
					return String.Empty;
				}
			}
		}

		/// <summary>
		/// CaveTubeからログアウトします。
		/// </summary>
		/// <param name="userId">ユーザーID</param>
		/// <param name="password">パスワード</param>
		/// <returns>ログアウトの成否</returns>
		/// <exception cref="System.ArgumentException" />
		/// <exception cref="System.Net.WebException" />
		public static async Task<Boolean> LogoutAsync(String userId, String password) {
			if (String.IsNullOrWhiteSpace(userId)) {
				var message = "UserIdが指定されていません。";
				throw new ArgumentException(message);
			}

			if (String.IsNullOrWhiteSpace(password)) {
				var message = "Passwordが指定されていません。";
				throw new ArgumentException(message);
			}

			using (var client = new WebClient()) {
				var data = new NameValueCollection {
					{"devkey", devkey},
					{"mode", "logout"},
					{"user", userId},
					{"pass", password},
				};

				var response = await client.UploadValuesTaskAsync(String.Format("{0}/api/auth", webUrl), "POST", data);
				return true;
			}
		}

		/// <summary>
		/// コメントサーバのアクセスキーを取得します。
		/// </summary>
		/// <param name="accessKey"></param>
		/// <returns></returns>
		public static async Task<String> GetAccessKeyAsync(String accessKey = null) {
			try {
				using (var client = new WebClient()) {
					client.Encoding = Encoding.UTF8;
					client.QueryString.Add("key", accessKey);
					var url = String.Format("{0}/accesskey", socketIOUrl);
					var jsonString = await client.DownloadStringTaskAsync(url);
					if (String.IsNullOrEmpty(jsonString)) {
						throw new CavetubeException("アクセスキーの取得に失敗しました。");
					}

					dynamic json = JObject.Parse(jsonString);
					return json.accessKey;
				}
			} catch (WebException e) {
				throw new CavetubeException("アクセスキーの取得に失敗しました。", e);
			}
		}
	}
}
