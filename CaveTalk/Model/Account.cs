namespace CaveTube.CaveTalk.Model {
	using System;
	using System.Collections.Generic;
	using CaveTube.CaveTalk.Utils;

	public sealed class Account {
		// MEMO Dapperが綺麗にキャッシュする仕様ならば、ここでキャッシュする必要はなくなります。
		private static IDictionary<String, Account> AccountCache = new Dictionary<String, Account>();

		public String AccountName { get; set; }
		public String Color { get; set; }
		public IEnumerable<Listener> Listeners {
			get {
				return Listener.GetListeners(this.AccountName);
			}
		}

		public void Save() {
			UpdateAccount(this);
		}

		public static Account GetAccount(String accountName) {
			if (AccountCache.ContainsKey(accountName)) {
				return AccountCache[accountName];
			}

			var account = DapperUtil.QueryFirst<Account>(@"
				SELECT
					AccountName
					,Account.Color
				FROM
					Account
				WHERE
					AccountName = @AccountName
				;
			", new {
				 AccountName = accountName,
			 });

			AccountCache[accountName] = account;

			return account;
		}

		public static void UpdateAccount(Account account) {
			DapperUtil.Execute(executor => {
				var transaction = executor.BeginTransaction();

				executor.Execute(@"
					INSERT OR REPLACE INTO Account (
						AccountName
						,Color
					) VALUES (
						@AccountName, @Color
					);
				", account, transaction);

				AccountCache[account.AccountName] = account;

				transaction.Commit();
			});
		}

		public static void CreateTable() {
			DapperUtil.Execute(@"
				CREATE TABLE IF NOT EXISTS Account (
					AccountName TEXT PRIMARY KEY  NOT NULL
					,Color TEXT
				);
			");
		}
	}
}
