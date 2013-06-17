namespace CaveTube.CaveTubeClient {
	using System;
	using System.Collections.Specialized;
	using System.Configuration;
	using System.Net;
	using System.Text;
	using Codeplex.Data;

	public static class CavetubeAuth {
		private static String webUrl = ConfigurationManager.AppSettings["web_server"] ?? "http://gae.cavelis.net";
		private static String devkey = ConfigurationManager.AppSettings["dev_key"] ?? String.Empty;

		/// <summary>
		/// CaveTubeにログインします。
		/// </summary>
		/// <param name="userId">ユーザー名</param>
		/// <param name="password">パスワード</param>
		/// <returns>APIキー</returns>
		/// <exception cref="System.ArgumentException" />
		/// <exception cref="System.Net.WebException" />
		public static String Login(String userId, String password) {
			if (String.IsNullOrWhiteSpace(userId)) {
				throw new ArgumentException("UserIdが指定されていません。");
			}

			if (String.IsNullOrWhiteSpace(password)) {
				throw new ArgumentException("Passwordが指定されていません。");
			}

			// ログイン処理に関しては同期処理にします。
			// 一度TPLパターンで実装しましたが、特に必要性を感じなかったので同期に戻しました。
			using (var client = new WebClient()) {
				var data = new NameValueCollection {
					{"devkey", devkey},
					{"mode", "login"},
					{"user", userId},
					{"pass", password},
				};

				try {
					var response = client.UploadValues(String.Format("{0}/api/auth", webUrl), "POST", data);
					var jsonString = Encoding.UTF8.GetString(response);

					var json = DynamicJson.Parse(jsonString);
					if (json.IsDefined("apikey") == false) {
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
		public static Boolean Logout(String userId, String password) {
			if (String.IsNullOrWhiteSpace(userId)) {
				var message = "UserIdが指定されていません。";
				throw new ArgumentException(message);
			}

			if (String.IsNullOrWhiteSpace(password)) {
				var message = "Passwordが指定されていません。";
				throw new ArgumentException(message);
			}

			// ログアウト処理に関しても同期処理にします。
			// 一度TPLパターンで実装しましたが、特に必要性を感じなかったので同期に戻しました。
			using (var client = new WebClient()) {
				var data = new NameValueCollection {
					{"devkey", devkey},
					{"mode", "logout"},
					{"user", userId},
					{"pass", password},
				};

				var response = client.UploadValues(String.Format("{0}/api/auth", webUrl), "POST", data);
				return true;
			}
		}
	}
}
