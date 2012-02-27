using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Media = System.Windows.Media;
using System.ComponentModel.DataAnnotations;

namespace CaveTube.CaveTalk.Model {
	public class Account {
		[Key]
		public String AccountName { get; set; }
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
		public virtual ICollection<Listener> Listeners { get; set; }
	}
}
