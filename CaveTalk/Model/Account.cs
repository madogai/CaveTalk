namespace CaveTube.CaveTalk.Model {
	using System;
	using System.Collections.Generic;
	using CaveTube.CaveTalk.Utils;

	public sealed class Account {
		public String AccountName { get; set; }
		public String Color { get; set; }
		public IEnumerable<Listener> Listeners {
			get {
				return Listener.GetListeners(this.AccountName);
			}
		}

		public static Account GetAccount(String accountName) {
			var result = DapperUtil.QueryFirst<Account>(@"
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
			return result;
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
