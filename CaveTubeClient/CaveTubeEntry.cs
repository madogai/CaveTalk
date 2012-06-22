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

		public static String RequestStartBroadcast(String title, String apiKey, String description, IEnumerable<String> tags, Boolean idVisible, Boolean anonymousOnly, Boolean testMode) {
			try {
				using (var client = new WebClient()) {
					var data = new NameValueCollection {
						{"title", title},
						{"apikey", apiKey},
						{"description", description},
						{"tags", String.Join(" ", tags)},
						{"id_visible", idVisible ? "true" : "false"},
						{"anonymous_only", anonymousOnly ? "true" : "false"},
						{"test_mode", testMode ? "true" : "false"},
						{"thumbnail", "false"},
					};

					var response = client.UploadValues(String.Format("{0}/api/start", webUrl), "POST", data);
					var jsonString = Encoding.UTF8.GetString(response);

					var json = DynamicJson.Parse(jsonString);
					if (json.IsDefined("ret") && json.ret == false) {
						return String.Empty;
					}

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
