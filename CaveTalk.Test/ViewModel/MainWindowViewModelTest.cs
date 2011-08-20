using CaveTube.CaveTalk.ViewModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using CaveTube.CaveTalk.CaveTubeClient;

namespace CaveTube.CaveTalk.Test {


	[TestClass()]
	public class MainWindowViewModelTest {
		MainWindowViewModel_Accessor target = new MainWindowViewModel_Accessor();

		[TestMethod]
		public void NotifyBalloon_正常() {
			// arrange
			var info = new LiveNotification("hoge", "全宇宙アップルパイ会議", "");

			// act
			target.NotifyLive(info);

			// assert
		}
	}
}
