namespace CaveTube.CaveTalk.Model {
	using System;
	using System.Collections.Generic;
	using CaveTube.CaveTalk.Utils;
	using Dapper;

	public sealed class Listener {
		// MEMO Dapperが綺麗にキャッシュする仕様ならば、ここでキャッシュする必要はなくなります。
		private static IDictionary<String, Listener> ListenerCache = new Dictionary<String, Listener>();

		public String ListenerId { get; set; }
		public String Name { get; set; }
		public String Color { get; set; }
		public String Author { get; set; }
		public String AccountName { get; set; }
		public Account Account {
			get {
				if (String.IsNullOrWhiteSpace(AccountName)) {
					return null;
				}
				return Model.Account.GetAccount(this.AccountName);
			}
		}

		public override Boolean Equals(Object obj) {
			var other = obj as Listener;
			if (other == null) {
				return false;
			}

			var sameListenerId = this.ListenerId == other.ListenerId;
			return sameListenerId;
		}

		public override Int32 GetHashCode() {
			if (this.ListenerId == null) {
				return base.GetHashCode();
			}

			return this.ListenerId.GetHashCode();
		}

		public void Save() {
			UpdateListener(this);
		}

		public static Listener GetListener(String listenerId) {
			if (ListenerCache.ContainsKey(listenerId)) {
				return ListenerCache[listenerId];
			};

			var listener = DapperUtil.QueryFirst<Listener>(@"
				SELECT
					ListenerId
					,Name
					,Color
					,Author
					,AccountName
				FROM
					Listener
				WHERE
					ListenerId = @ListenerId
				;
			", new {
				 ListenerId = listenerId,
			 });

			ListenerCache[listenerId] = listener;

			return listener;
		}

		public static IEnumerable<Listener> GetListeners(String accountName) {
			var listeners = DapperUtil.Query<Listener>(@"
				SELECT
					ListenerId
					,Name
					,Color
					,Author
					,AccountName
				FROM
					Listener
				WHERE
					AccountName = @AccountName
				;
			", new {
				 AccountName = accountName,
			 });
			return listeners;
		}

		public static void UpdateListener(Listener listener) {
			UpdateListener(new[] { listener });
		}

		public static void UpdateListener(IEnumerable<Listener> listeners) {
			DapperUtil.Execute(executor => {
				var transaction = executor.BeginTransaction();

				foreach (var listener in listeners) {
					executor.Execute(@"
						INSERT OR REPLACE INTO Listener (
							ListenerId
							,Name
							,Color
							,Author
							,AccountName
						) VALUES (
							@ListenerId, @Name, @Color, @Author, @AccountName
						);
					", listener, transaction);

					ListenerCache[listener.ListenerId] = listener;
				}

				transaction.Commit();
			});
		}

		public static void CreateTable() {
			DapperUtil.Execute(@"
				CREATE TABLE IF NOT EXISTS Listener (
					ListenerId TEXT PRIMARY KEY NOT NULL
					,Name TEXT
					,Color TEXT
					,Author TEXT NOT NULL
					,AccountName TEXT
				);
			");
		}
	}
}
