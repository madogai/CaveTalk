namespace CaveTube.CaveTubeClient {
	using System;
	using System.Collections.Specialized;
	using System.Configuration;
	using System.Net;
	using System.Text;
	using Codeplex.Data;

	public static class CavetubeAuth {
		private static String webUrl = ConfigurationManager.AppSettings["web_server"] ?? "http://gae.cavelis.net";

		/// <summary>
		/// CaveTubeにログインします。
		/// </summary>
		/// <param name="userId">ユーザー名</param>
		/// <param name="password">パスワード</param>
		/// <param name="devKey">開発者キー</param>
		/// <returns>APIキー</returns>
		/// <exception cref="System.ArgumentException" />
		/// <exception cref="System.Net.WebException" />
		public static String Login(String userId, String password, String devKey) {
			if (String.IsNullOrWhiteSpace(userId)) {
				throw new ArgumentException("UserIdが指定されていません。");
			}

			if (String.IsNullOrWhiteSpace(password)) {
				throw new ArgumentException("Passwordが指定されていません。");
			}

			if (String.IsNullOrWhiteSpace(devKey)) {
				throw new ArgumentException("DevKeyが指定されていません。");
			}

			// ログイン処理に関しては同期処理にします。
			// 一度TPLパターンで実装しましたが、特に必要性を感じなかったので同期に戻しました。
			using (var client = new WebClient()) {
				var data = new NameValueCollection {
					{"mode", "login"},
					{"devkey", devKey},
					{"user", userId},
					{"pass", password},
				};

				var response = client.UploadValues(String.Format("{0}/api/auth", webUrl), "POST", data);
				var jsonString = Encoding.UTF8.GetString(response);

				var json = DynamicJson.Parse(jsonString);
				if (json.IsDefined("ret") && json.ret == false) {
					return String.Empty;
				}

				if (json.IsDefined("apikey") == false) {
					return String.Empty;
				}

				return json.apikey;
			}
		}

		/// <summary>
		/// CaveTuneからログアウトします。
		/// </summary>
		/// <param name="userId">ユーザーID</param>
		/// <param name="password">パスワード</param>
		/// <param name="devKey">開発者キー</param>
		/// <returns>ログアウトの成否</returns>
		/// <exception cref="System.ArgumentException" />
		/// <exception cref="System.Net.WebException" />
		public static Boolean Logout(String userId, String password, String devKey) {
			if (String.IsNullOrWhiteSpace(userId)) {
				var message = "UserIdが指定されていません。";
				throw new ArgumentException(message);
			}

			if (String.IsNullOrWhiteSpace(password)) {
				var message = "Passwordが指定されていません。";
				throw new ArgumentException(message);
			}

			if (String.IsNullOrWhiteSpace(devKey)) {
				var message = "DevKeyが指定されていません。";
				throw new ArgumentException(message);
			}

			// ログアウト処理に関しても同期処理にします。
			// 一度TPLパターンで実装しましたが、特に必要性を感じなかったので同期に戻しました。
			using (var client = new WebClient()) {
				var data = new NameValueCollection {
					{"devkey", devKey},
					{"mode", "logout"},
					{"user", userId},
					{"pass", password},
				};

				var response = client.UploadValues(String.Format("{0}/api/auth", webUrl), "POST", data);
				var jsonString = Encoding.UTF8.GetString(response);
				var json = DynamicJson.Parse(jsonString);
				if (json.IsDefined("ret") && json.ret == false) {
					return false;
				}
				return true;
			}
		}
	}
}
