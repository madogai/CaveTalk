using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;

namespace CaveTube.CaveTalk.Model {
	public sealed class CaveTalkInitializer : IDatabaseInitializer<CaveTalkContext> {

		#region IDatabaseInitializer<CaveTalkContext> メンバー

		public void InitializeDatabase(CaveTalkContext context) {
			context.Database.CreateIfNotExists();
			if (context.Config.Any()) {
				return;
			}

			context.Config.Add(new Config {
				Id = Guid.NewGuid().ToString(),
				ApiKey = null,
				UserId = null,
				Password = null,
				SpeakApplication = (Int32)SpeakApplicationState.Bouyomi,
				SofTalkPath = null,
				NotifyPopupState = (Int32)NotifyPopupState.False,
				NotifyPopupTime = 5,
				NotifySoundFilePath = null,
				CommentPopupState = (Int32)CommentPopupState.Disable,
				CommentPopupTime = 5,
				ReadCommentName = false,
				ReadCommentNumber = false,
				FontSize = 12,
				TopMost = false,
			});
			context.SaveChanges();
		}

		#endregion
	}
}
