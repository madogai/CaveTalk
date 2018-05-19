namespace CaveTube.CaveTalk.Model {
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using CaveTube.CaveTalk.Utils;
	using Dapper;

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

		// アクセスキー
		private const String accessKey = "AccessKey";
		public String AccessKey {
			get {
				if (this.configDictionary.ContainsKey(accessKey) == false) {
					return String.Empty;
				}
				return this.configDictionary[accessKey];
			}
			set {
				if (String.IsNullOrWhiteSpace(value)) {
					this.configDictionary[accessKey] = String.Empty;
				}

				this.configDictionary[accessKey] = value;
			}
		}

		// APIキー
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

		// ユーザーID
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

		// パスワード
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

		// 読み上げアプリケーション
		private const String speakApplication = "SpeakApplication";
		public SpeakApplicationType SpeakApplication {
			get {
				SpeakApplicationType val;
				if (this.configDictionary.ContainsKey(speakApplication) && Enum.TryParse(this.configDictionary[speakApplication], out val)) {
					return val;
				}
				return SpeakApplicationType.Bouyomichan;
			}
			set {
				this.configDictionary[speakApplication] = value.ToString();
			}
		}

		// 棒読みちゃんのオプション有効化
		private const String enableBouyomiOption = "EnableBouyomiOption";
		public Boolean EnableBouyomiOption {
			get {
				Boolean val;
				if (this.configDictionary.ContainsKey(enableBouyomiOption) && Boolean.TryParse(this.configDictionary[enableBouyomiOption], out val)) {
					return val;
				}
				return false;
			}
			set {
				this.configDictionary[enableBouyomiOption] = value.ToString();
			}
		}

		// 棒読みちゃん 音量
		private const String bouyomiVolume = "BouyomiVolume";
		public Int32 BouyomiVolume {
			get {
				Int32 val;
				if (this.configDictionary.ContainsKey(bouyomiVolume) && Int32.TryParse(this.configDictionary[bouyomiVolume], out val)) {
					return val;
				}
				return 100;
			}
			set {
				this.configDictionary[bouyomiVolume] = value.ToString();
			}
		}

		// 棒読みちゃん 速度
		private const String bouyomiSpeed = "BouyomiSpeed";
		public Int32 BouyomiSpeed {
			get {
				Int32 val;
				if (this.configDictionary.ContainsKey(bouyomiSpeed) && Int32.TryParse(this.configDictionary[bouyomiSpeed], out val)) {
					return val;
				}
				return 100;
			}
			set {
				this.configDictionary[bouyomiSpeed] = value.ToString();
			}
		}

		// 棒読みちゃん 音程
		private const String bouyomiTone = "BouyomiTone";
		public Int32 BouyomiTone {
			get {
				Int32 val;
				if (this.configDictionary.ContainsKey(bouyomiTone) && Int32.TryParse(this.configDictionary[bouyomiTone], out val)) {
					return val;
				}
				return 100;
			}
			set {
				this.configDictionary[bouyomiTone] = value.ToString();
			}
		}

		// SofTalkのパス
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

		// ユーザーサウンドのパス
		private const String userSoundPath = "UserSoundPath";
		public String UserSoundFilePath {
			get {
				if (this.configDictionary.ContainsKey(userSoundPath) == false) {
					return String.Empty;
				}
				return this.configDictionary[userSoundPath];
			}
			set {
				this.configDictionary[userSoundPath] = value;
			}
		}

		// ユーザーサウンドの再生時間
		private const String userSoundTimeout = "UserSoundTimeout";
		public Decimal UserSoundTimeout {
			get {
				Decimal val;
				if (this.configDictionary.ContainsKey(userSoundTimeout) && Decimal.TryParse(this.configDictionary[userSoundTimeout], out val)) {
					return val;
				}
				return 1.0m;
			}
			set {
				this.configDictionary[userSoundTimeout] = value.ToString();
			}
		}

		// ユーザーサウンドのボリューム
		private const String userSoundVolume = "UserSoundVolume";
		public Double UserSoundVolume {
			get {
				Double val;
				if (this.configDictionary.ContainsKey(userSoundVolume) && Double.TryParse(this.configDictionary[userSoundVolume], out val)) {
					return val;
				}
				return 0.5;
			}
			set {
				this.configDictionary[userSoundVolume] = value.ToString();
			}
		}

		// 名前の読み上げ
		private const String readCommentName = "ReadCommentName";
		public Boolean ReadCommentName {
			get {
				Boolean val;
				if (this.configDictionary.ContainsKey(readCommentName) && Boolean.TryParse(this.configDictionary[readCommentName], out val)) {
					return val;
				}
				return false;
			}
			set {
				this.configDictionary[readCommentName] = value.ToString();
			}
		}

		// コメント番号の読み上げ
		private const String readCommentNumber = "ReadCommentNumber";
		public Boolean ReadCommentNumber {
			get {
				Boolean val;
				if (this.configDictionary.ContainsKey(readCommentNumber) && Boolean.TryParse(this.configDictionary[readCommentNumber], out val)) {
					return val;
				}
				return false;
			}
			set {
				this.configDictionary[readCommentNumber] = value.ToString();
			}
		}

		// コメントのポップアップ種別
		private const String commentPopupType = "CommentPopupType";
		public CommentPopupDisplayType CommentPopupType {
			get {
				CommentPopupDisplayType val;
				if (this.configDictionary.ContainsKey(commentPopupType) && Enum.TryParse(this.configDictionary[commentPopupType], out val)) {
					return val;
				}
				return CommentPopupDisplayType.None;
			}
			set {
				this.configDictionary[commentPopupType] = value.ToString();
			}
		}

		// コメントポップアップ時間
		private const String commentPopupTime = "CommentPopupTime";
		public Int32 CommentPopupTime {
			get {
				Int32 val;
				if (this.configDictionary.ContainsKey(commentPopupTime) && Int32.TryParse(this.configDictionary[commentPopupTime], out val)) {
					return val;
				}
				return 5;
			}
			set {
				this.configDictionary[commentPopupTime] = value.ToString();
			}
		}

		// Flashコメントジェネレーターの有効化
		private const String enableFlashCommentGenerator = "EnableFlashCommentGenerator";
		public Boolean EnableFlashCommentGenerator {
			get {
				Boolean val;
				if (this.configDictionary.ContainsKey(enableFlashCommentGenerator) && Boolean.TryParse(this.configDictionary[enableFlashCommentGenerator], out val)) {
					return val;
				}
				return false;
			}
			set {
				this.configDictionary[enableFlashCommentGenerator] = value.ToString();
			}
		}

		// FlashコメントジェネレーターDatファイルのパス
		private const String flashCommentGeneratorDatFilePath = "FlashCommentGeneratorDatFilePath";
		public String FlashCommentGeneratorDatFilePath {
			get {
				if (this.configDictionary.ContainsKey(flashCommentGeneratorDatFilePath) == false) {
					return String.Empty;
				}
				return this.configDictionary[flashCommentGeneratorDatFilePath];
			}
			set {
				this.configDictionary[flashCommentGeneratorDatFilePath] = value;
			}
		}

		// 配信終了時の通知
		private const String noticeLiveClose = "NoticeLiveClose";
		public Boolean NoticeLiveClose {
			get {
				Boolean val;
				if (this.configDictionary.ContainsKey(noticeLiveClose) && Boolean.TryParse(this.configDictionary[noticeLiveClose], out val)) {
					return val;
				}
				return false;
			}
			set {
				this.configDictionary[noticeLiveClose] = value.ToString();
			}
		}

		// フォントサイズ
		private const String fontSize = "FontSize";
		public Int32 FontSize {
			get {
				Int32 val;
				if (this.configDictionary.ContainsKey(fontSize) && Int32.TryParse(this.configDictionary[fontSize], out val)) {
					return val;
				}
				return 12;
			}
			set {
				this.configDictionary[fontSize] = value.ToString();
			}
		}

		// YouTube ストリームキー
		private const String youtubeStreamKey = "YouTubeStreamKey";
		public String YouTubeStreamKey {
			get {
				if (this.configDictionary.ContainsKey(youtubeStreamKey)) {
					return this.configDictionary[youtubeStreamKey];
				}
				return String.Empty;
			}
			set {
				this.configDictionary[youtubeStreamKey] = value;
			}
		}

		// YouTube ChannelId
		private const String youtubeChannelId = "YouTubeChannelId";
		public String YouTubeChannelId {
			get {
				if (this.configDictionary.ContainsKey(youtubeChannelId)) {
					return this.configDictionary[youtubeChannelId];
				}
				return String.Empty;
			}
			set {
				this.configDictionary[youtubeChannelId] = value;
			}
		}

		// Mixer ストリームキー
		private const String mixerStreamKey = "MixerStreamKey";
		public String MixerStreamKey {
			get {
				if (this.configDictionary.ContainsKey(mixerStreamKey)) {
					return this.configDictionary[mixerStreamKey];
				}
				return String.Empty;
			}
			set {
				this.configDictionary[mixerStreamKey] = value;
			}
		}

		// Mixer ユーザーID
		private const String mixerUserId = "MixerUserId";
		public String MixerUserId {
			get {
				if (this.configDictionary.ContainsKey(mixerUserId)) {
					return this.configDictionary[mixerUserId];
				}
				return String.Empty;
			}
			set {
				this.configDictionary[mixerUserId] = value;
			}
		}

		// StreamService
		private const String streamService = "StreamService";
		public String StreamService {
			get {
				if (this.configDictionary.ContainsKey(streamService)) {
					return this.configDictionary[streamService];
				}
				return String.Empty;
			}
			set {
				this.configDictionary[streamService] = value;
			}
		}

		// ウィンドウトップ固定
		private const String topMost = "TopMost";
		public Boolean TopMost {
			get {
				Boolean val;
				if (this.configDictionary.ContainsKey(topMost) && Boolean.TryParse(this.configDictionary[topMost], out val)) {
					return val;
				}
				return false;
			}
			set {
				this.configDictionary[topMost] = value.ToString();
			}
		}

		// ウィンドウTop
		private const String windowTop = "WindowTop";
		public Double WindowTop {
			get {
				Double val;
				if (this.configDictionary.ContainsKey(windowTop) && Double.TryParse(this.configDictionary[windowTop], out val)) {
					return val;
				}
				return Double.NaN;
			}
			set {
				this.configDictionary[windowTop] = value.ToString();
			}
		}

		// ウィンドウLeft
		private const String windowLeft = "WindowLeft";
		public Double WindowLeft {
			get {
				Double val;
				if (this.configDictionary.ContainsKey(windowLeft) && Double.TryParse(this.configDictionary[windowLeft], out val)) {
					return val;
				}
				return Double.NaN;
			}
			set {
				this.configDictionary[windowLeft] = value.ToString();
			}
		}

		// ウィンドウの高さ
		private const String windowHeight = "WindowHeight";
		public Double WindowHeight {
			get {
				Double val;
				if (this.configDictionary.ContainsKey(windowHeight) && Double.TryParse(this.configDictionary[windowHeight], out val)) {
					return val;
				}
				return 450;
			}
			set {
				this.configDictionary[windowHeight] = value.ToString();
			}
		}

		// ウィンドウの横幅
		private const String windowWidth = "WindowWidth";
		public Double WindowWidth {
			get {
				Double val;
				if (this.configDictionary.ContainsKey(windowWidth) && Double.TryParse(this.configDictionary[windowWidth], out val)) {
					return val;
				}
				return 525;
			}
			set {
				this.configDictionary[windowWidth] = value.ToString();
			}
		}

		// Idカラムの表示
		private const String displayIdColumn = "DisplayIdColumn";
		public Boolean DisplayIdColumn {
			get {
				Boolean val;
				if (this.configDictionary.ContainsKey(displayIdColumn) && Boolean.TryParse(this.configDictionary[displayIdColumn], out val)) {
					return val;
				}
				return true;
			}
			set {
				this.configDictionary[displayIdColumn] = value.ToString();
			}
		}

		// Iconカラムの表示
		private const String displayIconColumn = "DisplayIconColumn";
		public Boolean DisplayIconColumn {
			get {
				Boolean val;
				if (this.configDictionary.ContainsKey(displayIconColumn) && Boolean.TryParse(this.configDictionary[displayIconColumn], out val)) {
					return val;
				}
				return false;
			}
			set {
				this.configDictionary[displayIconColumn] = value.ToString();
			}
		}

		// 投稿時間カラムの表示
		private const String displayPostTimeColumn = "DisplayPostTimeColumn";
		public Boolean DisplayPostTimeColumn {
			get {
				Boolean val;
				if (this.configDictionary.ContainsKey(displayPostTimeColumn) && Boolean.TryParse(this.configDictionary[displayPostTimeColumn], out val)) {
					return val;
				}
				return true;
			}
			set {
				this.configDictionary[displayPostTimeColumn] = value.ToString();
			}
		}

		// 経過時間カラムの表示
		private const String displayElapsedPostTimeColumn = "DisplayElapsedPostTimeColumn";
		public Boolean DisplayElapsedPostTimeColumn {
			get {
				Boolean val;
				if (this.configDictionary.ContainsKey(displayElapsedPostTimeColumn) && Boolean.TryParse(this.configDictionary[displayElapsedPostTimeColumn], out val)) {
					return val;
				}
				return true;
			}
			set {
				this.configDictionary[displayElapsedPostTimeColumn] = value.ToString();
			}
		}

		// ソート順
		private const String sortDirection = "SortDirection";
		public ListSortDirection? SortDirection {
			get {
				ListSortDirection val;
				if (this.configDictionary.ContainsKey(sortDirection) && Enum.TryParse<ListSortDirection>(this.configDictionary[sortDirection], out val)) {
					return val;
				}
				return null;
			}
			set {
				this.configDictionary[sortDirection] = value.ToString();
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
			Bouyomichan, SofTalk, UserSound, NoSound,
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
