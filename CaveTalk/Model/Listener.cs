using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Media = System.Windows.Media;
using System.ComponentModel.DataAnnotations;

namespace CaveTube.CaveTalk.Model {
	public class Listener {
		[Key]
		public String ListenerId { get; set; }
		public String Name { get; set; }
		public String Color { get; set; }
		[NotMapped]
		public Media.Brush BackgroundColor {
			get {
				return (Media.Brush)new Media.BrushConverter().ConvertFrom(this.Color);
			}
			set {
				this.Color = value.ToString();
			}
		}
		public String Author { get; set; }
		public Account Account { get; set; }

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
	}
}
