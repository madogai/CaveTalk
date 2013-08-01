namespace CaveTube.CaveTubeClient {
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Configuration;
	using System.Net;
	using System.Text;
	using Codeplex.Data;
	using System.Xml;

	public static class CaveTubeEntry {
		private static String webUrl = ConfigurationManager.AppSettings["web_server"] ?? "http://gae.cavelis.net";
		private static String devkey = ConfigurationManager.AppSettings["dev_key"] ?? String.Empty;

		public static StartInfo RequestStartBroadcast(String title, String apiKey, String description, IEnumerable<String> tags, Int32 thumbnailSlot, Boolean idVisible, Boolean anonymousOnly, Boolean loginOnly, Boolean testMode, String socketId) {
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

					var response = client.UploadValues(String.Format("{0}/api/start", webUrl), "POST", data);
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

		public static UserData RequestUserData(String apiKey) {
			try {
				using (var client = new WebClient()) {
					client.Encoding = Encoding.UTF8;

					var jsonString = client.DownloadString(String.Format("{0}/api/user_data?devkey={1}&apikey={2}", webUrl, devkey, apiKey));
					var json = DynamicJson.Parse(jsonString);
					return new UserData(json);
				}
			} catch (WebException) {
				return new UserData();
			} catch (XmlException) {
				return new UserData();
			}
		}

		public static IEnumerable<Genre> RequestGenre(String apiKey) {
			try {
				using (var client = new WebClient()) {
					client.Encoding = Encoding.UTF8;

					var jsonString = client.DownloadString(String.Format("{0}/api/genre?devkey={1}&apikey={2}", webUrl, devkey, apiKey));
					var json = DynamicJson.Parse(jsonString);
					return ((dynamic[])json.genres).Select(genre => new Genre(genre));
				}
			} catch (WebException) {
				return Enumerable.Empty<Genre>();
			} catch (XmlException) {
				return Enumerable.Empty<Genre>();
			}
		}

		public class UserData {
			public IEnumerable<Thumbnail> Thumbnails { get; private set; }

			internal UserData() {
				this.Thumbnails = Enumerable.Empty<Thumbnail>();
			}

			internal UserData(dynamic json) {
				this.Thumbnails = ((dynamic[])json.thumbnails).Select(t => new Thumbnail(t));
			}
		}

		public class Genre {
			public String Title { get; private set; }
			public IEnumerable<String> Tags { get; private set; }

			internal Genre(dynamic json) {
				this.Title = json.title;
				this.Tags = ((dynamic[])json.tags).Select(t => (String)t);
			}
		}

		public class Thumbnail {
			public Int32 Slot { get; private set; }
			public String Url { get; private set; }

			internal Thumbnail(dynamic json) {
				this.Slot = (Int32)json.slot;
				this.Url = json.url;
			}
		}

		public class StartInfo {
			public String StreamName { get; private set; }
			public String WarnMessage { get; private set; }

			internal StartInfo(dynamic json) {
				this.StreamName = json.stream_name;
				this.WarnMessage = json.warn_message;
			}
		}
	}
}
