using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CaveTube.CaveTalk.Model {
	public class Message {
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public Int32 Order { get; set; }
		public Int32 Number { get; set; }
		public String Name { get; set; }
		public String Comment { get; set; }
		public DateTime PostTime { get; set; }
		public Boolean IsAuth { get; set; }
		public Boolean IsBan { get; set; }
		public Room Room { get; set; }
		public Listener Listener { get; set; }
		[NotMapped]
		public Boolean IsAsciiArt {
			get {
				return Regex.IsMatch(this.Comment, "　 (?!<br>|$)");
			}
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
	}
}
