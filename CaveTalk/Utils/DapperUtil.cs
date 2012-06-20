namespace CaveTube.CaveTalk.Utils {
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Data.Common;
	using System.Linq;
	using Dapper;
	using System.Configuration;

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

		public static void Execute(Action<QueryExecutor> action) {
			ExecuteDbAction(conn => {
				var executor = new QueryExecutor(conn);
				action(executor);
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

		private static void ExecuteDbAction(Action<IDbConnection> act) {
			using (IDbConnection conn = factory.CreateConnection()) {
				try {
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

		public sealed class QueryExecutor {
			private IDbConnection conn;

			public QueryExecutor(IDbConnection conn) {
				this.conn = conn;
			}

			public IDbTransaction BeginTransaction() {
				return conn.BeginTransaction();
			}

			public IDbTransaction BeginTransaction(IsolationLevel il) {
				return conn.BeginTransaction(il);
			}

			public int Execute(String sql, dynamic param = null, IDbTransaction transaction = null, Int32? commandTimeout = null, CommandType? commandType = null) {
				return Dapper.SqlMapper.Execute(conn, sql, param, transaction, commandTimeout, commandType);
			}

			public IEnumerable<T> Query<T>(String sql, dynamic param = null, IDbTransaction transaction = null, Boolean buffered = true, Int32? commandTimeout = null, CommandType? commandType = null) {
				return Dapper.SqlMapper.Query(conn, sql, param, transaction, buffered, commandTimeout, commandType);
			}

			public IEnumerable<dynamic> Query(String sql, dynamic param = null, IDbTransaction transaction = null, Boolean buffered = true, Int32? commandTimeout = null, CommandType? commandType = null) {
				return Dapper.SqlMapper.Query(conn, sql, param, transaction, buffered, commandTimeout, commandType);
			}

			public IEnumerable<TReturn> Query<TFirst, TSecond, TReturn>(String sql, Func<TFirst, TSecond, TReturn> map, dynamic param = null, IDbTransaction transaction = null, Boolean buffered = true, String splitOn = "Id", Int32? commandTimeout = null, CommandType? commandType = null) {
				return Dapper.SqlMapper.Query(conn, sql, param, transaction, buffered, commandTimeout, commandType);
			}

			public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(String sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, Boolean buffered = true, String splitOn = "Id", Int32? commandTimeout = null, CommandType? commandType = null) {
				return Dapper.SqlMapper.Query(conn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
			}

			public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn>(String sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, dynamic param = null, IDbTransaction transaction = null, Boolean buffered = true, String splitOn = "Id", Int32? commandTimeout = null, CommandType? commandType = null) {
				return Dapper.SqlMapper.Query(conn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
			}

			public IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TReturn>(String sql, Func<TFirst, TSecond, TThird, TReturn> map, dynamic param = null, IDbTransaction transaction = null, Boolean buffered = true, String splitOn = "Id", Int32? commandTimeout = null, CommandType? commandType = null) {
				return Dapper.SqlMapper.Query(conn, sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
			}

			public SqlMapper.GridReader QueryMultiple(String sql, dynamic param = null, IDbTransaction transaction = null, Int32? commandTimeout = null, CommandType? commandType = null) {
				return Dapper.SqlMapper.QueryMultiple(conn, sql, param, transaction, commandTimeout, commandType);
			}
		}
	}
}
