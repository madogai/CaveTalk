namespace CaveTube.CaveTalk.Utils {
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Data;
	using System.Data.Common;
	using System.Linq;
	using Dapper;

	internal static class DapperUtil {
		private static DbProviderFactory factory;
		private static String connectionString;

		static DapperUtil() {
			factory = DbProviderFactories.GetFactory("System.Data.SQLite");
			//factory = DbProviderFactories.GetFactory("System.Data.SqlServerCe.4.0");
			connectionString = ConfigurationManager.ConnectionStrings["SQLiteConnection"].ConnectionString;
			//connectionString = ConfigurationManager.ConnectionStrings["SQLServerCeConnection"].ConnectionString;
		}

		public static void Execute(String query, Object param = null) {
			ExecuteDbAction(conn => {
				conn.Execute(query, param);
			});
		}

		public static void Execute(Action<IDbConnection> action) {
			ExecuteDbAction(conn => {
				action(conn);
			});
		}

		public static TOut QueryFirst<TOut>(String query, Object param = null) {
			var results = Query<TOut>(query, param);
			return results.FirstOrDefault();
		}

		public static IEnumerable<TOut> Query<TOut>(String query, Object param = null) {
			return ExecuteDbAction(conn => {
				var results = conn.Query<TOut>(query, param);
				return results;
			});
		}

		public static void Vacuum() {
			ExecuteDbAction(conn => {
				conn.Execute(@"
					VACUUM;
				");
			});
		}

		private static void ExecuteDbAction(Action<IDbConnection> act) {
			using (IDbConnection conn = factory.CreateConnection()) {
				try {
					conn.ConnectionString = connectionString;
					conn.Open();
					act(conn);
				}
				finally {
					conn.Close();
				}
			}
		}

		private static TOut ExecuteDbAction<TOut>(Func<IDbConnection, TOut> act) {
			using (IDbConnection conn = factory.CreateConnection()) {
				try {
					conn.ConnectionString = connectionString;
					conn.Open();
					return act(conn);
				}
				finally {
					conn.Close();
				}
			}
		}
	}
}
