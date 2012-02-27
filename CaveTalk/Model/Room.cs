using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace CaveTube.CaveTalk.Model {
	public class Room {
		[Key]
		public String RoomId { get; set; }
		public String Author { get; set; }
		public String Title { get; set; }
		public DateTime StartTime { get; set; }
		public virtual ICollection<Message> Messages { get; set; }
		[NotMapped]
		public Int32 ListenerCount { get; set; }
	}
}
