namespace CaveTube.CaveTalk.Model {
	using System;
	using System.Collections.Generic;
	using CaveTube.CaveTalk.Utils;

	public sealed class Message {
		public Int32? Order { get; set; }
		public Int32 Number { get; set; }
		public String Name { get; set; }
		public String Comment { get; set; }
		public DateTime PostTime { get; set; }
		public Boolean IsAuth { get; set; }
		public Boolean IsBan { get; set; }
		public String RoomId { get; set; }
		public String ListenerId { get; set; }

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

		public static IEnumerable<Message> GetMessages(Room room) {
			var messages = DapperUtil.Query<Message>(@"
				SELECT
					Order
					,Number
					,Name
					,Comment
					,PostTime
					,IsAuth
					,IsBan
					,RoomId
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
					if (message.Order == null) {
						executor.Execute(@"
							INSERT Message (
								Number
								,Name
								,Comment
								,PostTime
								,IsAuth
								,IsBan
								,RoomId
								,ListenerId
							) VALUES (
								@Number, @Name, @Comment, @PostTime, @IsAuth, @IsBan, @RoomId, @ListenerId
							);
						", message, transaction);
					}
					else {
						executor.Execute(@"
							UPDATE Message
							SET
								Number = @Number
								,Name = @Name
								,Comment = @Comment
								,PostTime = @PostTime
								,IsAuth = @IsAuth
								,IsBan = @IsBan
								,RoomId = @RoomId
								,ListenerId = @ListenerId
							WHERE
								Order = @Order
							;
						", message, transaction);
					}
				}
				transaction.Commit();
			});
		}
	}
}
