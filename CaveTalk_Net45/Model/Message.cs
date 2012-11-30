namespace CaveTube.CaveTalk.Model {
	using System;
	using System.Collections.Generic;
	using CaveTube.CaveTalk.Utils;

	public sealed class Message {
		public Int64? Order { get; set; }
		public Int64 Number { get; set; }
		public String Name { get; set; }
		public String Comment { get; set; }
		public DateTime PostTime { get; set; }
		public Boolean IsAuth { get; set; }
		public Boolean IsBan { get; set; }
		public String RoomId { get; set; }
		public String ListenerId { get; set; }
		public Listener Listener {
			get {
				if(String.IsNullOrWhiteSpace(this.ListenerId)) {
					return null;
				};
				return Listener.GetListener(this.ListenerId); }
		}

		public override Boolean Equals(Object obj) {
			var other = obj as Message;
			if (other == null) {
				return false;
			}

			var sameOrder = this.Order == other.Order;
			return sameOrder;
		}

		public override Int32 GetHashCode() {
			return this.Order.GetHashCode();
		}

		public void Save() {
			UpdateMessage(this);
		}

		public static IEnumerable<Message> GetMessages(Room room) {
			var messages = DapperUtil.Query<Message>(@"
				SELECT
					RoomId
					,PostTime
					,Number
					,Name
					,Comment
					,IsAuth
					,IsBan
					,ListenerId
				FROM
					Message
				WHERE
					RoomId = @RoomId
				ORDER BY
					Order
				;
			", room);
			return messages;
		}

		public static void UpdateMessage(Message message) {
			UpdateMessage(new[] { message });
		}

		public static void UpdateMessage(IEnumerable<Message> messages) {
			DapperUtil.Execute(executor => {
				var transaction = executor.BeginTransaction();
				foreach (var message in messages) {
					executor.Execute(@"
						INSERT OR REPLACE INTO Message (
							RoomId
							,PostTime
							,Number
							,Name
							,Comment
							,IsAuth
							,IsBan
							,ListenerId
						) VALUES (
							@RoomId, @PostTime, @Number, @Name, @Comment, @IsAuth, @IsBan, @ListenerId
						);
					", message, transaction);
				}
				transaction.Commit();
			});
		}

		public static void CreateTable() {
			DapperUtil.Execute(@"
				CREATE TABLE IF NOT EXISTS Message (
					RoomId TEXT NOT NULL
					,PostTime DATETIME NOT NULL
					,Number INTEGER NOT NULL 
					,Name TEXT
					,Comment TEXT NOT NULL
					,IsAuth BOOL NOT NULL
					,IsBan BOOL NOT NULL
					,ListenerId TEXT
					,PRIMARY KEY (
						RoomId
						,PostTime
					)
				);
			");
		}
	}
}
