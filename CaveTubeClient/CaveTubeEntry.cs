namespace CaveTube.CaveTubeClient {
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Configuration;
	using System.Net;
	using System.Text;
	using Codeplex.Data;

	public static class CaveTubeEntry {
		private static String webUrl = ConfigurationManager.AppSettings["web_server"] ?? "http://gae.cavelis.net";
		private static String devkey = ConfigurationManager.AppSettings["dev_key"] ?? String.Empty;

		public static String RequestStartBroadcast(String title, String apiKey, String description, IEnumerable<String> tags, Boolean idVisible, Boolean anonymousOnly, Boolean loginOnly, Boolean testMode, String socketId) {
			try {
				using (var client = new WebClient()) {
					var data = new NameValueCollection {
						{"devkey", devkey},
						{"apikey", apiKey},
						{"title", title},
						{"description", description},
						{"tag", String.Join(" ", tags)},
						{"id_visible", idVisible ? "true" : "false"},
						{"anonymous_only", anonymousOnly ? "true" : "false"},
						{"login_only", loginOnly ? "true" : "false"},
						{"test_mode", testMode ? "true" : "false"},
						{"socket_id", socketId},
						{"thumbnail", "false"},
					};

					var response = client.UploadValues(String.Format("{0}/api/start", webUrl), "POST", data);
					var jsonString = Encoding.UTF8.GetString(response);

					var json = DynamicJson.Parse(jsonString);
					if (json.IsDefined("stream_name") == false) {
						return String.Empty;
					}

					return json.stream_name;
				}
			} catch (WebException) {
				return String.Empty;
			}
		}
	}
}
