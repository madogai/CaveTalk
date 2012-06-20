namespace CaveTube.CaveTalk.Model {
	using System;
	using System.Collections.Generic;
	using CaveTube.CaveTalk.Utils;

	public sealed class Room {
		public String RoomId { get; set; }
		public String Author { get; set; }
		public String Title { get; set; }
		public DateTime StartTime { get; set; }
		public Int32 ListenerCount { get; set; }
		public IEnumerable<Message> Messages {
			get {
				return Message.GetMessages(this);
			}
		}

		public static Room GetRoom(String roomId) {
			var result = DapperUtil.QueryFirst<Room>(@"
				SELECT
					RoomId
					,Author
					,Title
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

		public static void UpdateRoom(Room room) {
			DapperUtil.Execute(executor => {
				var transaction = executor.BeginTransaction();

				executor.Execute(@"
					INSERT OR REPLACE INTO Room (
						RoomId
						,Author
						,Title
						,StartTime
						,ListenerCount
					) VALUES (
						@RoomId, @Author, @Title, @StartTime, @ListenerCount
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
					,StartTime DATETIME NOT NULL
					,ListenerCount INTEGER NOT NULL
				);
			");
		}
	}
}
