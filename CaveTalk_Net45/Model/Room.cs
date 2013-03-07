namespace CaveTube.CaveTalk.Model {
	using System;
	using System.Collections.Generic;
	using CaveTube.CaveTalk.Utils;

	public sealed class Room {
		public String RoomId { get; set; }
		public String Author { get; set; }
		public String Title { get; set; }
		public String Description { get; set; }
		public String Tags { get; set; }
		public Boolean IdVisible { get; set; }
		public Boolean AnonymousOnly { get; set; }
		public Boolean LoginOnly { get; set; }
		public DateTime StartTime { get; set; }
		public Int64 ListenerCount { get; set; }
		public IEnumerable<Message> Messages {
			get {
				return Message.GetMessages(this);
			}
		}

		public void Save() {
			UpdateRoom(this);
		}

		public static Room GetRoom(String roomId) {
			var result = DapperUtil.QueryFirst<Room>(@"
				SELECT
					RoomId
					,Author
					,Title
					,Description
					,Tags
					,IdVisible
					,AnonymousOnly
					,StartTime
					,ListenerCount
				FROM
					Room
				WHERE
					RoomId = @RoomId
				;
			", new {
				 RoomId = roomId,
			 });
			return result;
		}

		public static IEnumerable<Room> GetRooms(String accountName) {
			var rooms = DapperUtil.Query<Room>(@"
				SELECT
					RoomId
					,Author
					,Title
					,Description
					,Tags
					,IdVisible
					,AnonymousOnly
					,StartTime
					,ListenerCount
				FROM
					Room
				WHERE
					Author = @Author
				ORDER BY
					StartTime DESC
				;
			", new {
				 Author = accountName,
			 });
			return rooms;
		}

		public static void UpdateRoom(Room room) {
			DapperUtil.Execute(executor => {
				var transaction = executor.BeginTransaction();

				executor.Execute(@"
					INSERT OR REPLACE INTO Room (
						RoomId
						,Author
						,Title
						,Description
						,Tags
						,IdVisible
						,AnonymousOnly
						,StartTime
						,ListenerCount
					) VALUES (
						@RoomId, @Author, @Title, @Description, @Tags, @IdVisible, @AnonymousOnly, @StartTime, @ListenerCount
					);
				", room, transaction);

				transaction.Commit();
			});
		}

		public static void CreateTable() {
			DapperUtil.Execute(@"
				CREATE TABLE IF NOT EXISTS Room (
					RoomId TEXT PRIMARY KEY  NOT NULL
					,Author TEXT NOT NULL
					,Title TEXT NOT NULL
					,Description TEXT
					,Tags TEXT
					,IdVisible BOOL NOT NULL
					,AnonymousOnly BOOL NOT NULL
					,LoginOnly BOOL
					,StartTime DATETIME NOT NULL
					,ListenerCount INTEGER NOT NULL
				);
			");
		}
	}
}
