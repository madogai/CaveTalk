namespace CaveTube.CaveTalk.Model {
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using CaveTube.CaveTalk.Utils;

	public sealed class Config {
		private static Config config;

		public static Config GetConfig() {
			if (config != null) {
				return config;
			}

			config = new Config();
			var dict = DapperUtil.Query<dynamic>(@"
				SELECT
					ConfigKey
					,Value
				FROM
					Config
				;
			").ToDictionary(row => (String)row.ConfigKey, row => (String)row.Value);
			config.configDictionary = dict;

			return config;
		}

		private IDictionary<String, String> configDictionary;

		private Config() {
			configDictionary = new Dictionary<String, String>();
		}

		private const String apiKey = "ApiKey";
		public String ApiKey {
			get {
				if (this.configDictionary.ContainsKey(apiKey) == false) {
					return String.Empty;
				}
				return this.configDictionary[apiKey];
			}
			set {
				if (String.IsNullOrWhiteSpace(value)) {
					this.configDictionary[apiKey] = String.Empty;
				}

				this.configDictionary[apiKey] = value;
			}
		}

		private const String userId = "UserId";
		public String UserId {
			get {
				if (this.configDictionary.ContainsKey(userId) == false) {
					return String.Empty;
				}
				return this.configDictionary[userId];
			}
			set {
				if (String.IsNullOrWhiteSpace(value)) {
					this.configDictionary[userId] = String.Empty;
				}

				this.configDictionary[userId] = value;
			}
		}

		private const String password = "Password";
		public String Password {
			get {
				if (this.configDictionary.ContainsKey(password) == false) {
					return String.Empty;
				}
				return this.configDictionary[password];
			}
			set {
				if (String.IsNullOrWhiteSpace(value)) {
					this.configDictionary[password] = String.Empty;
				}

				this.configDictionary[password] = value;
			}
		}

		private const String speakApplication = "SpeakApplication";
		public SpeakApplicationType SpeakApplication {
			get {
				if (this.configDictionary.ContainsKey(speakApplication) == false) {
					return SpeakApplicationType.Bouyomichan;
				}
				return (SpeakApplicationType)Enum.Parse(typeof(SpeakApplicationType), this.configDictionary[speakApplication]);
			}
			set {
				this.configDictionary[speakApplication] = value.ToString();
			}
		}

		private const String sofTalkPath = "SofTalkPath";
		public String SofTalkPath {
			get {
				if (this.configDictionary.ContainsKey(sofTalkPath) == false) {
					return String.Empty;
				}
				return this.configDictionary[sofTalkPath];
			}
			set {
				this.configDictionary[sofTalkPath] = value;
			}
		}

		private const String commentPopupType = "CommentPopupType";
		public CommentPopupDisplayType CommentPopupType {
			get {
				if (this.configDictionary.ContainsKey(commentPopupType) == false) {
					return CommentPopupDisplayType.None;
				}
				return (CommentPopupDisplayType)Enum.Parse(typeof(CommentPopupDisplayType), this.configDictionary[commentPopupType]);
			}
			set {
				this.configDictionary[commentPopupType] = value.ToString();
			}
		}

		private const String commentPopupTime = "CommentPopupTime";
		public Int32 CommentPopupTime {
			get {
				if (this.configDictionary.ContainsKey(commentPopupTime) == false) {
					return 5;
				}
				return Int32.Parse(this.configDictionary[commentPopupTime]);
			}
			set {
				this.configDictionary[commentPopupTime] = value.ToString();
			}
		}

		private const String readCommentName = "ReadCommentName";
		public Boolean ReadCommentName {
			get {
				if (this.configDictionary.ContainsKey(readCommentName) == false) {
					return false;
				}
				return Boolean.Parse(this.configDictionary[readCommentName]);
			}
			set {
				this.configDictionary[readCommentName] = value.ToString();
			}
		}

		private const String readCommentNumber = "ReadCommentNumber";
		public Boolean ReadCommentNumber {
			get {
				if (this.configDictionary.ContainsKey(readCommentNumber) == false) {
					return false;
				}
				return Boolean.Parse(this.configDictionary[readCommentNumber]);
			}
			set {
				this.configDictionary[readCommentNumber] = value.ToString();
			}
		}

		private const String fontSize = "FontSize";
		public Int32 FontSize {
			get {
				if (this.configDictionary.ContainsKey(fontSize) == false) {
					return 12;
				}
				try {
					return Int32.Parse(this.configDictionary[fontSize]);
				}
				catch (FormatException) {
					return 12;
				}
			}
			set {
				this.configDictionary[fontSize] = value.ToString();
			}
		}

		private const String topMost = "TopMost";
		public Boolean TopMost {
			get {
				if (this.configDictionary.ContainsKey(topMost) == false) {
					return false;
				}
				return Boolean.Parse(this.configDictionary[topMost]);
			}
			set {
				this.configDictionary[topMost] = value.ToString();
			}
		}

		public void Save() {
			DapperUtil.Execute(executor => {
				var transaction = executor.BeginTransaction();
				foreach (var item in this.configDictionary) {
					executor.Execute(@"
						INSERT OR REPLACE INTO Config (
							ConfigKey
							,Value
						) VALUES (
							@ConfigKey, @Value
						);
					", new {
						ConfigKey = item.Key,
						Value = item.Value
					}, transaction);
				}

				transaction.Commit();
			});
		}

		public enum SpeakApplicationType {
			Bouyomichan, SofTalk,
		}

		public enum CommentPopupDisplayType {
			Always, Minimize, None,
		}

		public static void CreateTable() {
			DapperUtil.Execute(@"
				CREATE TABLE IF NOT EXISTS Config (
					ConfigKey TEXT PRIMARY KEY NOT NULL
					,Value TEXT NOT NULL
				);
			");
		}
	}
}
