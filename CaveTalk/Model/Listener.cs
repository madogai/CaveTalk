namespace CaveTube.CaveTalk.Model {
	using System;
	using System.Collections.Generic;
	using CaveTube.CaveTalk.Utils;

	public sealed class Listener {
		public String ListenerId { get; set; }
		public String Name { get; set; }
		public String Color { get; set; }
		public String Author { get; set; }
		public String AccountName { get; set; }
		public Account Account {
			get { return Model.Account.GetAccount(this.AccountName); }
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

		public static Listener GetListener(String listenerId) {
			var listener = DapperUtil.QueryFirst<Listener>(@"
				SELECT
					ListenerId
					,Name
					,Color
					,Author
				FROM
					Listener
				WHERE
					ListenerId = @ListenerId
				;
			", new {
				 ListenerId = listenerId,
			 });
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
						DELETE FROM
							Listener
						WHERE
							ListenerId = @ListenerId
						;
					", listener, transaction);

					executor.Execute(@"
						INSERT OR REPACE INTO Listener (
							ListenerId
							,Name
							,Color
							,Author
							,AccountName
						) VALUES (
							@ListenerId, @Name, @Color, @Author, @AccountName
						);
					", listener, transaction);
				}

				transaction.Commit();
			});
		}
	}
}
