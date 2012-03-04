using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Media = System.Windows.Media;
using System.ComponentModel.DataAnnotations;

namespace CaveTube.CaveTalk.Model {
	public class Config {
		public String Id { get; set; }

		public String ApiKey { get; set; }
		public String UserId { get; set; }
		public String Password { get; set; }

		public Int32 SpeakApplication { get; set; }
		public String SofTalkPath { get; set; }

		public Int32 CommentPopupState { get; set; }
		public Int32 CommentPopupTime { get; set; }

		public Boolean ReadCommentNumber { get; set; }
		public Boolean ReadCommentName { get; set; }

		public Int32 NotifyPopupState { get; set; }
		public Int32 NotifyPopupTime { get; set; }
		public String NotifySoundFilePath { get; set; }

		public Int32 FontSize { get; set; }
		public Boolean TopMost { get; set; }
	}
}
